from fastmcp import FastMCP
from fastmcp.resources import Resource
import logging
from typing import Dict, Any
from utils import file_utils
import subprocess
import os
from pydantic import BaseModel, Field
from pathlib import Path
from enum import Enum
from core.modes.pipelines import old_chatbot_replicated

class Phases(Enum):
     INIT = 1
     COMPILE = 2
     CORRECTNESS = 3
     WAITING = 4

# Create an MCP server
mcp = FastMCP("PLang")

class State:
    def __init__(self):
          self.current_phase = Phases.INIT
          
    def get_phase(self):
         return self.current_phase
    
    def set_phase(self, new_phase):
         self.current_phase = new_phase

state = State()
# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='[%(levelname)s] %(message)s'
)
logger = logging.getLogger(__name__)

@mcp.tool(
    name="syntax_helper",
    description="Get syntax help for P language constructs"
)
def handle_syntax_help(topic: str) -> Dict[str, Any]:
        # TODO: Implement RAG
        logger.info(f"[TOOL CALLED] handle_syntax_help")
        logger.info(f"\t arg(topic) = {topic}")
        syntax_guide_content = file_utils.read_file("resources/context_files/P_syntax_guide.txt")
        return {"result": f"{syntax_guide_content}"}


class PCompileParams(BaseModel):
     path: str = Field(..., description="This is the ABSOLUTE path to the project directory or .p file. A project directory must contain a .pproj file.")

@mcp.tool(
    name="p_compile",
    description="Compile a P project or a .p file"
)
async def handle_compilation(params: PCompileParams) -> Dict[str, Any]:


    def try_compile(ppath, stdout_file=None, stderr_file=None):
        p = Path(ppath)
        flags = ['-pf', ppath, "-o", str(p.parent)] if p.is_file() else []

        final_cmd_arr = ['p', 'compile', *flags]
        result = subprocess.run(final_cmd_arr, capture_output=True, cwd=str(p) if not p.is_file() else None)
        
        if stdout_file:
            with open(stdout_file, 'wb') as f:
                f.write(result.stdout)
        
        if stderr_file:
            with open(stderr_file, 'wb') as f:
                f.write(result.stderr)
        
        return result.returncode == 0, " ".join(final_cmd_arr), result.stdout, result.stderr
    
    success, final_cmd, stdout, stderr = try_compile(params.path)

    return {"success":success, "final_cmd": final_cmd, "stdout":stdout, "stderr":stderr}



class DesignDocToPProjParams(BaseModel):
     path: str = Field(..., description="Absolute path to the design doc file. Accepted file types: .txt")
     out_dir: str = Field(..., description="Absolute path to the output directory where the created project should be stored.")
     log_dir: str = Field(..., description="Directory where the server will log its progress.")

@mcp.tool(
    name="Design Document to P Project",
    description="Given a design document, this tool will implement a P project that adheres to it."
)
def designdoc_to_pproj(params: DesignDocToPProjParams):
    dd_path = params.path
    out_dir = params.out_dir
    log_dir = params.log_dir
    
    state.set_phase("compile")
    (pipeline, final_out_dir, compile_success) = old_chatbot_replicated(dd_path, out_dir=out_dir)
    return {"success": compile_success, "stage": state.get_phase(), "final_out_dir":final_out_dir, "token_usage": pipeline.get_token_usage()}


class FeedbackHandlerParams(BaseModel):
    response_dict: dict = Field(..., description="A json dictionary containing the response to the last feedback request.")

@mcp.tool(
    name="Feedback Handler",
    description="This tool should be used to handle feedback requests from the MCP server."
)
def handle_feedback(feedback: FeedbackHandlerParams):
     response_dict = feedback.response_dict
     
     phase = state.get_phase()



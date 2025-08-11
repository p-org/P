import os, logging, shutil
from pathlib import Path
from core.pipelining.prompting_pipeline import PromptingPipeline
from utils import file_utils
import pytest
import subprocess
import json
# from legacy.chatbot_modes import DesignDocInputMode
from glob import glob
from sentence_transformers import SentenceTransformer
from utils.constants import CLAUDE_3_7

logger = logging.getLogger(__name__)

# CLAUDE_3_7 = "us.anthropic.claude-3-7-sonnet-20250219-v1:0"
SAVED_GLOBAL_STATE = None

# RAG_MODEL_aMLML6v2 = SentenceTransformer('all-MiniLM-L6-v2')
# RAG_MODEL_aMLML12v2 = SentenceTransformer('all-MiniLM-L12-v2')



def test1(ddoc = "/Users/ahmayun/Desktop/mcp/test-env/LightSwitch_DesignDoc.txt", out_dir = "/Users/ahmayun/Desktop/mcp/test-env/output"):
    from core.modes.pipelines import old_chatbot_replicated
    old_chatbot_replicated(ddoc, out_dir=out_dir)


class MockStatus:
    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        pass

    def write(self, msg):
        pass

    def update(self, **kwargs):
        pass

class MockContainer:        
    def status(self, msg, expanded=True):
        return MockStatus()

def tag_surround(tagname, contents):
    return f"<{tagname}>\n{contents}\n</{tagname}>"

@pytest.fixture
def doc_list():
    modular_dir = Path("resources/context_files/modular")
    return [str(path) for path in modular_dir.glob("*.txt")]

def tag_surround_relative(path, contents):
    tag_name = str(Path(*Path(path).parts[-2:]))
    return f"<{tag_name}>\n{contents}\n</{tag_name}>"

# @pytest.mark.parametrize("benchmark_dir", ["resources/p-model-benchmark/8_dd2psrcA/"])
# def test_taskgen_dd2psrc(benchmark_dir):
#     task_dirs = glob(f"{benchmark_dir}/*")
#     tasks = []
#     for task_dir_path in task_dirs:
#         task_name = Path(task_dir_path).stem
#         design_doc = glob(f"{task_dir_path}/*.txt")[0]
#         prompt_pre = "Here are the PSpec and PTst files:\n"
#         prompt_post = "\n\nGenerate the code for file(s) that should be in PSrc, that satisfy this test configuration. All the types, enums, and events you need are already declared in PSrc/Enums_Types_Events.p.\n"
#         p_files = glob(f"{task_dir_path}/PSpec/*.p") + glob(f"{task_dir_path}/PTst/*.p")
#         print(f"p_files = {p_files}")
#         full_prompt = file_utils.combine_files(p_files, pre=prompt_pre, post=prompt_post, preprocessing_function=tag_surround_relative)
#         task = (task_name, design_doc, full_prompt)
#         tasks.append(task)
#     print(f"tasks = {tasks}")
#     return tasks

@pytest.mark.parametrize("benchmark_dir", ["resources/p-model-benchmark/8_dd2psrcA/"])
def test_taskgen_dd2psrc(benchmark_dir):
    task_dirs = glob(f"{benchmark_dir}/*")
    tasks = []
    for task_dir_path in task_dirs:
        task_name = Path(task_dir_path).stem
        globout = glob(f"{task_dir_path}/*.txt")
        print(f"globout = {globout}")
        design_doc = globout[0]
        # p_files = glob(f"{task_dir_path}/PSpec/*.p") + glob(f"{task_dir_path}/PTst/*.p")
        task = (task_name, design_doc, task_dir_path)
        tasks.append(task)
    print(f"tasks = {tasks}")
    return tasks

def taskgen_dd2proj(design_docs_dir):
    design_docs = glob(f"{design_docs_dir}/*.txt")
    tests = list(map(lambda dd: (Path(dd).stem, dd), design_docs))
    print(f"DETECTED TESTS: {tests}")
    return tests


# =======================================================================================
from utils import global_state, log_utils, compile_utils, regex_utils
import re
from utils.generate_p_code import extract_filenames, extract_validate_and_log_Pcode, get_context_files

LIST_OF_MACHINE_NAMES = 'list_of_machine_names'
LIST_OF_FILE_NAMES = 'list_of_file_names'
ENUMS_TYPES_EVENTS = 'enums_types_events'
MACHINE = 'machine'
MACHINE_STRUCTURE = "MACHINE_STRUCTURE"
PROJECT_STRUCTURE="PROJECT_STRUCTURE"

PSRC = 'PSrc'
PSPEC = 'PSpec'
PTST = 'PTst'


instructions = {
        LIST_OF_MACHINE_NAMES: file_utils.read_file(global_state.initial_instructions_path),
        LIST_OF_FILE_NAMES: file_utils.read_file(global_state.generate_filenames_instruction_path),
        ENUMS_TYPES_EVENTS: file_utils.read_file(global_state.generate_enums_types_events_instruction_path),
        MACHINE_STRUCTURE: file_utils.read_file(global_state.generate_machine_structure_path),
        PROJECT_STRUCTURE: file_utils.read_file(global_state.generate_project_structure_path),
        MACHINE: file_utils.read_file(global_state.generate_machine_instruction_path),
        PSRC: file_utils.read_file(global_state.generate_modules_file_instruction_path),
        PSPEC: file_utils.read_file(global_state.generate_spec_files_instruction_path),
        PTST: file_utils.read_file(global_state.generate_test_files_instruction_path)
}

def get_context_files():
    return {
        "ENUMS_TYPES_EVENTS": [
            global_state.P_ENUMS_GUIDE,
            global_state.P_TYPES_GUIDE,
            global_state.P_EVENTS_GUIDE
        ],
        "MACHINE_STRUCTURE": [
            global_state.P_MACHINES_GUIDE
        ],
        "MACHINE": [
            global_state.P_STATEMENTS_GUIDE
        ],
        PSRC: [
            global_state.P_MODULE_SYSTEM_GUIDE
        ],
        PSPEC: [
            global_state.P_SPEC_MONITORS_GUIDE
        ],
        PTST: [
            global_state.P_TEST_CASES_GUIDE
        ]
    }

def get_recent_project_path():

    if global_state.custom_dir_path != None:
        file_path = os.path.join(global_state.custom_dir_path, global_state.project_name_with_timestamp)
        return file_path
    if file_utils.check_directory(global_state.recent_dir_path):
        for proj in file_utils.list_top_level_contents(global_state.recent_dir_path):
            file_path = os.path.join(os.getcwd(), global_state.recent_dir_path + "/" + proj)
            return file_path
    return None

def compiler_analysis(model_id, pipeline, all_responses, num_of_iterations, ctx_pruning=None):
    max_iterations = num_of_iterations
    recent_project_path = file_utils.get_recent_project_path()

    compilation_success, compilation_result = compile_utils.compile_Pcode(recent_project_path)
    if compilation_success:
        logger.info(f":white_check_mark: :green[Compilation succeeded in {max_iterations - num_of_iterations} iterations.]")
        logger.info(f"Compilation succeeded in {max_iterations - num_of_iterations} iterations.")
        return
    
    P_filenames_dict = regex_utils.get_all_P_files(compilation_result)
    while (not compilation_success and num_of_iterations > 0):
        file_name, line_number, column_number = compile_utils.parse_compile_error(compilation_result)
        logger.info(f". . . :red[[Iteration #{(max_iterations - num_of_iterations)}] Compilation failed in {file_name} at line {line_number}:{column_number}. Fixing the error...]")
        # 1. Obtain the error message's information. 
        custom_msg, file_path, file_contents = compile_utils.get_correction_instruction(P_filenames_dict, compilation_result)
        
        # Continue the conversation to fix compiler errors
        # Apply context pruning before fixing errors
        if ctx_pruning:
            original_messages = messages.copy()
            messages = ctx_pruning.prune_context(messages, file_name)
            ctx_pruning.log_context_metrics(original_messages, messages, MockStatus())
            
        pipeline.add_user_msg(custom_msg)
        response = pipeline.invoke_llm(model_id, candidates=1, heuristic='random')
        logger.info(f". . . . . . Compiling the fixed code...")
        # Get token usage from response
        # token_usage = response["current_tokens"]
        # backend_status.write(f". . . . . . Token usage - Stage: Input {token_usage['inputTokens']}, Output {token_usage['outputTokens']} | Cumulative: Input {globals.model_metrics['inputTokens']}, Output {globals.model_metrics['outputTokens']}")
        log_filename, Pcode = extract_validate_and_log_Pcode(response, "", "", logging_enabled=False)
        if log_filename is not None and Pcode is not None:
            log_utils.log_Pcode_to_file(Pcode, file_path)
            all_responses[log_filename] = Pcode
        num_of_iterations -= 1

        # Log the diff
        log_utils.log_code_diff(file_contents, response, "After fixing the P code")
        compilation_success, compilation_result = compile_utils.compile_Pcode(recent_project_path)
        logger.info("============================DEBUG NOW ===========")
        logger.info(f"COMPILATION RESULT : {compilation_result}")
        logger.info(f"COMPILATION STATUS : {compilation_success}")    
    if compilation_success: 
        logger.info(f":green[Compilation succeeded in {max_iterations - num_of_iterations} iterations.]")
        # backend_status.write(f":green[Total compilation token usage - Input: {globals.model_metrics['inputTokens']} tokens, Output: {globals.model_metrics['outputTokens']} tokens]")
        logger.info(f"Compilation succeeded in {max_iterations - num_of_iterations} iterations.")
    
    global_state.compile_iterations += (max_iterations - num_of_iterations)

    global_state.compile_success = compilation_success

def generate_machine_code(model, pipeline, instructions, filename, dirname):
    """Generate machine code using either two-stage or single-stage process."""
    # Stage 1: Generate structure
    pipeline.add_user_msg(instructions['MACHINE_STRUCTURE'].format(machineName=filename))
    pipeline.add_documents_inline(get_context_files()["MACHINE_STRUCTURE"], tag_surround)

    stage1_response = pipeline.invoke_llm(model, candidates=1, heuristic='random')    
    structure_pattern = r'<structure>(.*?)</structure>'
    match = re.search(structure_pattern, stage1_response, re.DOTALL)
    
    if match:
        # Two-stage generation
        machine_structure = match.group(1).strip()
        logger.info(f"  . . . Stage 2: Implementing function bodies for {filename}.p")
        
        pipeline.add_user_msg(instructions[MACHINE].format(machineName=filename)+ "\n\nHere is the starting structure:\n\n" + machine_structure)
        pipeline.add_documents_inline(get_context_files()["MACHINE"], tag_surround)

        response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
        logger.info(response)
    else:
        # Fallback to single-stage
        logger.info(f"  . . . :red[Failed to extract structure for {filename}.p. Falling back to single-stage generation.]")
        pipeline.add_user_msg(instructions[MACHINE].format(machineName=filename))
        pipeline.add_documents_inline(get_context_files()["MACHINE"], tag_surround)
        response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
        logger.info(response)

    # token_usage = response["current_tokens"]
    # log_token_usage(token_usage, backend_status)
    
    return extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, dirname)


def generate_generic_file(model_id, pipeline, instructions, filename, dirname):

    pipeline.add_user_msg(instructions[dirname].format(filename=filename))
    pipeline.add_documents_inline(get_context_files()[dirname], tag_surround)
    response = pipeline.invoke_llm(model_id, candidates=1, heuristic='random')
    
    return extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, dirname)

def generate_generic_file_mock(model_id, pipeline, instructions, filename, dirname, benchmark_dir):

    pipeline.add_user_msg(instructions[dirname].format(filename=filename))
    pipeline.add_documents_inline(get_context_files()[dirname], tag_surround)

    mock_response = tag_surround(f"{filename}.p", file_utils.read_file(f"{benchmark_dir}/{dirname}/{filename}.p"))
    pipeline.add_assistant_msg(mock_response)
    response = pipeline.get_last_response()

    # response = pipeline.invoke_llm(model_id, candidates=1, heuristic='random')
    
    return extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, dirname)


def create_proj_files(project_root):
    
    # Create project structure with folders and pproj file
    from utils.project_structure_utils import setup_project_structure
    parent_abs_path = global_state.custom_dir_path or os.path.join(os.getcwd(), global_state.recent_dir_path)
    logger.info("Step 0: Creating project structure...")
    setup_project_structure(project_root, global_state.project_name)
    logger.info(f":white_check_mark: Project structure created at: {project_root}")
  
def mock_generate_pproj_file(project_root, project_name, benchmark_dir):
    pproj_file = glob(f"{benchmark_dir}/*.pproj")[0]
    pproj_content = file_utils.read_file(pproj_file)
    
    pproj_path = os.path.join(project_root, f"{project_name}.pproj")
    file_utils.write_file(pproj_path, pproj_content)
    print("Created Project Structure :white_check_mark:")
    
    return pproj_path

def mock_setup_project_structure(project_root, project_name, benchmark_dir):
    from utils.project_structure_utils import create_project_directories
    directories = create_project_directories(project_root)
    
    # Create .pproj file
    pproj_path = mock_generate_pproj_file(project_root, project_name, benchmark_dir)
    
    return {
        "directories": directories,
        "pproj_file": pproj_path
    }

def mock_create_proj_files(project_root, benchmark_dir):
    
    # Create project structure with folders and pproj file
    logger.info("Step 0: Creating project structure...")
    mock_setup_project_structure(project_root, global_state.project_name, benchmark_dir)
    logger.info(f":white_check_mark: Project structure created at: {project_root}")
  

def test_dd2proj_current(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    from utils.chat_utils import generate_response
    global SAVED_GLOBAL_STATE
    SAVED_GLOBAL_STATE = save_module_state(global_state)


    global_state.temperature = temperature
    global_state.model_id = model
    
    dd_name, dd_path = task
    destination_path = ".tmp"
    if destination_path and destination_path.strip() != "" and file_utils.check_directory(destination_path.strip()):
        global_state.custom_dir_path = destination_path.strip()
    
    design_doc_content = file_utils.read_file(dd_path)
    user_inp = "User uploaded Design Document: " + dd_name
    global_state.chat_history.add_exchange("user", None, user_inp, None)

    generate_response(design_doc_content, MockContainer())
    project_root = f"{destination_path}/{global_state.project_name_with_timestamp}"

    return project_root


from utils.module_utils import save_module_state, restore_module_state
@pytest.mark.parametrize("task", [("1_lightswitch", "resources/p-model-benchmark/3_designdoc2pprojA/1_lightswitch.txt")])
def test_dd2proj_replicated(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, out_dir=".tmp", **kwargs):

    global SAVED_GLOBAL_STATE
    SAVED_GLOBAL_STATE = save_module_state(global_state)
    
    all_responses = {}
    
    _, dd_path = task
    dd_content = file_utils.read_file(dd_path)
    
    project_name_pattern = r'<title>(.*?)</title>'

    match = re.search(project_name_pattern, dd_content, re.IGNORECASE)
    if match:
        global_state.project_name = match.group(1).strip().replace(" ", "_")
    
    timestamp = global_state.current_time.strftime('%Y_%m_%d_%H_%M_%S')
    global_state.project_name_with_timestamp = f"{global_state.project_name}_{timestamp}"
    log_utils.move_recent_to_archive()
    file_utils.empty_file(global_state.full_log_path)
    file_utils.empty_file(global_state.code_diff_log_path)
    
    destination_path = ".tmp"
    if destination_path and destination_path.strip() != "" and file_utils.check_directory(destination_path.strip()):
        global_state.custom_dir_path = destination_path.strip()

    parent_abs_path = global_state.custom_dir_path or os.path.join(os.getcwd(), global_state.recent_dir_path)
    
    project_root = os.path.join(parent_abs_path, global_state.project_name_with_timestamp)

    create_proj_files(project_root)

    pipeline = PromptingPipeline()
    system_prompt = file_utils.read_file(global_state.system_prompt_path)
    pipeline.add_system_prompt(system_prompt)

    # Get initial machine list
    text = instructions[LIST_OF_MACHINE_NAMES].format(userText=dd_content)
    pipeline.add_user_msg(text, [global_state.P_basics_path])
    pipeline.add_user_msg("These are the example P Programs ",[global_state.P_program_example_path])
    machines_list = pipeline.invoke_llm(model, candidates=1, heuristic='random')
    # Generate filenames
    pipeline.add_user_msg(instructions[LIST_OF_FILE_NAMES])
    response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
    global_state.filenames_map = extract_filenames(response)
    # Generate enums, types, and events
    pipeline.add_user_msg(instructions[ENUMS_TYPES_EVENTS])
    pipeline.add_documents_inline(get_context_files()["ENUMS_TYPES_EVENTS"], tag_surround)
    response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
    log_filename, Pcode = extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, PSRC)
    
    file_abs_path = os.path.join(project_root, PSRC, log_filename)
    logger.info(f":blue[. . . filepath: {file_abs_path}]")
    
    if log_filename is not None and Pcode is not None:
        all_responses[log_filename] = Pcode

    step_no = 2

    for dirname, filenames in global_state.filenames_map.items():
        if dirname != PSRC:
            logger.info(f"Step {step_no}: Generating {dirname}")
            step_no += 1
            
        for filename in filenames:
            logger.info(f"Generating file {filename}.p")
            if dirname == PSRC and filename in machines_list:
                log_filename, Pcode = generate_machine_code(model, pipeline, instructions, filename, dirname)
            else:
                log_filename, Pcode = generate_generic_file(model, pipeline, instructions, filename, dirname)

            log_file_full_path = os.path.join(project_root, dirname, log_filename)
            logger.info(f":blue[. . . filepath: {log_file_full_path}]")
            if log_filename is not None and Pcode is not None:
                all_responses[log_filename] = Pcode

        logger.info(f"Running the P compiler and analyzer on {dirname}...")
        num_iterations = 20 if dirname == PTST else 15
        compiler_analysis(model, pipeline, all_responses, num_iterations)
    
    # return all_responses
    return (pipeline, project_root)

def create_base_pipeline_fewshot():
    p_few_shot = 'resources/context_files/modular-fewshot/p-fewshot-formatted.txt'
    p_nuances = 'resources/context_files/p_nuances.txt'

    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary."
    
    initial_msg = 'Here are some information relevant to P.'
    
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg, documents = [p_few_shot,p_nuances])
    return pipeline

def taskgen_pchecker_fix_single(generated_dir):
    p = Path(generated_dir)
    test_name = p.parent.stem

    return [(f"{test_name}", generated_dir)]

# results_dir should be the top level dir like key-results/semantic/2025-07-27-16-26-24
def taskgen_pchecker_fix(results_dir):
    tasks = []
    # Find all trial directories
    trial_dirs = glob(f"{results_dir}/trial_*")
    
    # For each trial directory
    for trial_dir in trial_dirs:
        # Find all test folders (non-empty directories) in the trial directory
        test_dirs = [d for d in glob(f"{trial_dir}/*") if os.path.isdir(d)]
        
        # For each test directory
        for test_dir in test_dirs:
            # Get test name from directory name
            test_name = f"{Path(test_dir).parent.name}/{Path(test_dir).name}"
            # Create path to generated code
            generated_dir = os.path.join(test_dir, "generated")
            
            # Only add if generated directory exists
            if os.path.exists(generated_dir):
                tasks.append((test_name, generated_dir))
    
    print(f"TASKS: {tasks}")
    return tasks


def attempt_fix_pchecker_error(pipeline, project_state, file_dict):
    return project_state

def file_dict_to_prompt(file_dict, pre="", post=""):
    result = pre
    
    for filepath, contents in file_dict.items():
        result += f"<{filepath}>\n{contents}\n</{filepath}>\n"
    
    result += post
    return result

# def attempt_fix_pchecker_errors(pipeline: PromptingPipeline, current_project_state, test_case, trace_dict, trace_log, out_dir):

#     prompt = file_dict_to_prompt(current_project_state, pre="Here are the project files\n", post=f"\Attached is the error trace when the test case {test_case} is run:\n")
#     new_project_state = {}
#     pipeline.add_user_msg(f"""{prompt}
# Trace:\n{trace_log}

# What do you think the issue is? Format your response as follows:
# <Short logical description of error>
# filename: description of fix
# ...
# """)
#     pipeline.invoke_llm()
#     with open(f"{out_dir}/llm_analysis.txt", "w") as f:
#         analysis = pipeline.get_last_response()
#         f.write(analysis)
#         print("===== ANALYSIS ======")
#         print(analysis)
#         print("="*20)
#         input("press ENTER to continue...")
        

#     # Ask LLM to provide fixed code
#     pipeline.add_user_msg("""Based on your analysis above, provide the complete fixed code for each file that needs to be modified.
# Format your response with the complete file contents wrapped in XML tags using the filename, like:
# <PSrc/Machine.p>
# [complete fixed file content]
# </PSrc/Machine.p>
# """)
#     pipeline.invoke_llm()
#     response = pipeline.get_last_response()
    
#     # Save the fix attempt
#     with open(f"{out_dir}/llm_fix.txt", "w") as f:
#         f.write(response)
    
#     print(f"Wrote LLM fix to {out_dir}/llm_fix.txt. Go take a look")
#     input("press ENTER to continue...")

#     # Extract updated files from response
#     for line in response.split('\n'):
#         if line.startswith('<') and not line.startswith('</') and line.endswith('>'):
#             # Found start tag
#             filename = line[1:-1]  # Remove < and >
#             content = []
#         elif line.startswith('</'):
#             # Found end tag, save the content
#             if content:
#                 new_project_state[filename] = '\n'.join(content)
#         else:
#             # Collecting content lines
#             content.append(line)
    
#     return new_project_state

# def test_fix_pchecker_errors(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
#     user_quit = False
#     max_attempts = 10
#     test_name, project_root = task
#     out_dir = f"{kwargs['out_dir']}/{test_name}"

#     attempt = 0
#     pipeline = create_base_pipeline_fewshot()


#     prev_project_root = project_root
#     new_project_root = file_utils.make_copy(project_root, out_dir)
#     current_project_state = file_utils.capture_project_state(new_project_root)
#     checker_out, trace_dicts, trace_logs = {}, {}, {}
#     while True:

#         attempt_dir = f"{out_dir}/attempt_{attempt}"
#         print(f"COMPILING: {new_project_root}")
#         compilable = try_compile(new_project_root, f"{attempt_dir}/std_streams")
#         if not compilable:
#             new_project_root = prev_project_root
#             print("NOT COMPILABLE")
#             continue
#         else:
#             print(f"CHECKING: {new_project_root}")
#             checker_out, trace_dicts, trace_logs = try_pchecker(new_project_root, f"{attempt_dir}/std_streams")
#             prev_project_root = new_project_root
#             print(f"CHECKER RESULT: {checker_out}")

#         if not trace_dicts.keys():
#             # There are no pchecker bugs to fix
#             break

#         if user_quit or attempt > max_attempts:
#             break

#         new_project_root = f"{attempt_dir}/modified"
#         test_case = list(trace_dicts.keys())[0]
#         print(f"FIXING TEST: {test_case}")
#         trace_dict = trace_dicts[test_case]
#         trace_log = trace_logs[test_case]

#         new_state = attempt_fix_pchecker_errors(pipeline, current_project_state, test_case, trace_dict, trace_log, out_dir=attempt_dir)
        
#         current_project_state = {**current_project_state, **new_state}

#         file_utils.write_project_state(current_project_state, new_project_root)
        
#         attempt += 1

#     return (pipeline, new_project_root)

def reduce_trace_size(trace_log):
    lines = trace_log.splitlines()
    reduced = "\n".join(lines[-50:])
    return reduced

def generate_generic_analysis_prompt(p_code, trace_log):
    with open(global_state.generate_files_to_fix_for_pchecker, "r") as file:
        template = file.read()
        llm_query = template.format(
            error_trace=reduce_trace_size(trace_log),
            p_code=p_code, 
            tool="PChecker",
            additional_error_info="The line that starts with \"<ErrorLog>\" has the error message"
            )
    return llm_query

def generate_hot_state_analysis_prompt(test_case, p_code, trace_log):
    with open(global_state.template_path_hot_state_bug_analysis, "r") as file:
        template = file.read()
        llm_query = template.format(
            error_trace=reduce_trace_size(trace_log),
            p_code=p_code, 
            tool="PChecker",
            additional_error_info="The line that starts with \"<ErrorLog>\" has the error message. You are only given the last 50 lines."
            )
    return llm_query
    

# def generate_analysis_prompt(p_code, test_case, trace_log, error_category):
#     """Generate the prompt for analyzing P checker errors."""
#     llm_query = ""
#     if error_category == ErrorCategories.ENDED_IN_HOT_STATE:
#         llm_query = generate_hot_state_analysis_prompt(test_case, p_code, trace_log)
#     else:
#         llm_query = generate_generic_analysis_prompt(p_code, trace_log)
#     return llm_query

def generate_analysis_prompt(p_code, test_case, trace_log, error_category):
    """Generate the prompt for analyzing P checker errors."""
    llm_query = generate_generic_analysis_prompt(p_code, trace_log)
    return llm_query

def request_and_save_analysis(pipeline, prompt, out_dir, error_category):
    """Request error analysis from LLM and save the response."""
    pipeline.add_user_msg(prompt)
    pipeline.invoke_llm()
    analysis = pipeline.get_last_response()
    
    with open(f"{out_dir}/llm_analysis.txt", "w") as f:
        f.write(analysis)

    return analysis

def process_llm_response_with_tags(llm_analysis):
    pattern = r'<([^>]+)>(.*?)</\1>'
    matches = re.findall(pattern, llm_analysis, re.DOTALL)
    return {tag: content.strip() for tag, content in matches}

def generate_fix_prompt(p_code, analysis, error_category):
    with open(global_state.generate_fixed_file_for_pchecker, "r") as f:
        template = f.read()
        params = {"p_code": p_code, "fix_description": analysis}
        llm_query = template.format(**params)
    return llm_query

def request_and_save_fix(pipeline, prompt, out_dir, error_category):
    pipeline.add_user_msg(f"{prompt}\n\nApply the recommended fixes")
    pipeline.invoke_llm()
    response = pipeline.get_last_response()
    with open(f"{out_dir}/llm_fix.txt", "w") as f:
        f.write(response)

    return process_llm_response_with_tags(response)

def parse_fix_response(response):
    """Parse the fix response and extract updated files."""
    new_project_state = {}
    content = []
    current_filename = None
    inside_tags = False
    
    for line in response.split('\n'):
        if line.startswith('<') and not line.startswith('</') and line.endswith('>'):
            # Found start tag
            current_filename = line[1:-1]  # Remove < and >
            inside_tags = True
            content = []
        elif line.startswith('</'):
            # Found end tag, save the content
            if content and current_filename:
                new_project_state[current_filename] = '\n'.join(content)
        else:
            # Collecting content lines
            content.append(line)
    
    return new_project_state

def attempt_fix_pchecker_errors(pipeline: PromptingPipeline, current_project_state, test_case, trace_dict, trace_log, error_category, out_dir):
    """Attempt to fix P checker errors by getting analysis and fixes from LLM."""
    # Generate prompt and get analysis
    p_code = file_dict_to_prompt(current_project_state)
    prompt = generate_analysis_prompt(p_code, test_case, trace_log, error_category)
    analysis = request_and_save_analysis(pipeline, prompt, out_dir, error_category)
    
    # Get and save fix
    
    fix_prompt = generate_fix_prompt(p_code, analysis, error_category)
    fix_response = request_and_save_fix(pipeline, fix_prompt, out_dir, error_category)

    return fix_response

def setup_fix_pipeline():
    """Create and setup the pipeline for fixing P checker errors."""
    pipeline = create_base_pipeline_fewshot()
    # pipeline = create_base_old_pipeline()
    return pipeline

def setup_project_state(project_root, out_dir):
    """Setup initial project state."""
    new_project_root = file_utils.make_copy(project_root, out_dir, new_name="original_project")
    current_project_state = file_utils.capture_project_state(new_project_root)
    return new_project_root, current_project_state

def attempt_fix_iteration(pipeline, current_project_state, test_case, trace_dict, trace_log, error_category, attempt_dir):
    """Run one iteration of the fix attempt."""
    new_state = attempt_fix_pchecker_errors(
        pipeline, current_project_state, test_case, trace_dict, trace_log, error_category=error_category, out_dir=attempt_dir
    )
        
    return {**current_project_state, **new_state}


def get_failing_test_names(result_dict):
    failing_tests = []
    for test_name in result_dict:
        if not result_dict[test_name]:
            failing_tests.append(test_name)

    return failing_tests

from enum import Enum

class ErrorCategories(Enum):
    DEADLOCK = 1
    UNHANDLED_EVENT = 2
    ENDED_IN_HOT_STATE = 3
    FAILED_ASSERTION = 4
    EXCEPTION = 5
    UNKNOWN = 6

def categorize_error(log):
    """Categorize an error log based on its content."""
    log_lower = log.lower()
    if "deadlock detected" in log_lower:
        return ErrorCategories.DEADLOCK
    elif "received event" in log_lower and "cannot be handled" in log_lower:
        return ErrorCategories.UNHANDLED_EVENT
    elif "in hot state" in log_lower and "at the end of program" in log_lower:
        return ErrorCategories.ENDED_IN_HOT_STATE
    elif "assertion failed" in log_lower:
        return ErrorCategories.FAILED_ASSERTION
    elif "exception" in log_lower:
        return ErrorCategories.EXCEPTION
    else:
        return ErrorCategories.UNKNOWN

def identify_error_category(trace_str):
    error_lines = re.findall(r'<ErrorLog> (.*?)$', trace_str, re.MULTILINE)
    return categorize_error(error_lines[0])
    

def compute_progress_percentage(i, total):
    return f"{i/total*100:.2f}"

from utils import checker_utils
def test_fix_pchecker_errors(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    total_progress = compute_progress_percentage(kwargs["task_number"], kwargs["total_tasks"])
    print(f"==== TASK {total_progress}% =====")
    """Test fixing P checker errors with multiple attempts."""
    user_quit = False
    max_attempts = 3
    test_name, project_root = task
    test_name_dir = f"{kwargs['out_dir']}/{test_name}"
    out_dir = f"{test_name_dir}/fix_attempts"

    # Setup
    pipeline = setup_fix_pipeline()
    prev_project_root = project_root
    new_project_root, current_project_state = setup_project_state(project_root, test_name_dir)
    prev_project_state = current_project_state
    
    
    orig_results, _, orig_trace_logs = checker_utils.try_pchecker(project_root, "/tmp")
    test_cases_to_fix = get_failing_test_names(orig_results)
    subprocess.run(['cp', '-r', f'{project_root}/../std_streams', f"{test_name_dir}/original_std_streams"])
    all_tests_fixed = False
    latest_results = {}
    latest_trace_logs = {}
    total_tokens_input = 0
    total_tokens_output = 0
    grace_points = 1.0

    for i, test_case in enumerate(test_cases_to_fix):
        attempt = 0
        prev_error_category = identify_error_category(orig_trace_logs[test_case])
        error_category = prev_error_category

        
        if all_tests_fixed:
            break

        while attempt < max_attempts and not user_quit:
            print(f"==== ({total_progress}%) TASK SUB-PROGRESS {compute_progress_percentage(i+(attempt/(max_attempts+0.1)), len(test_cases_to_fix))}% ====")
            print(f"FIXING TEST: {test_case}\nATTEMPT {attempt}")
            attempt_dir = f"{out_dir}/{test_case}/attempt_{attempt}"
            
            current_results, trace_dicts, trace_logs = try_pchecker(new_project_root, f"{attempt_dir}/std_streams")
            latest_results = current_results
            latest_trace_logs = trace_logs
            prev_project_root = new_project_root
            
            if not trace_dicts.keys():
                all_tests_fixed = True
                print("ALL TESTS FIXED!")
                break  # No more errors to fix
            
            if test_case not in trace_dicts:
                print(f"TEST CASE FIXED: {test_case}")
                break # Current error has been fixed


            # Setup for next fix attempt
            new_project_root = f"{attempt_dir}/modified"
            # test_case = list(trace_dicts.keys())[0]

            prev_error_category = error_category
            error_category = identify_error_category(trace_logs[test_case])

            if error_category != prev_error_category:
                # Give the model a few more chances if it made some progress
                awarded = round(grace_points)
                max_attempts += awarded
                grace_points -= 0.1
                print(f"Awarded {awarded} grace point for changed error.")
                print(f"Remaning grace points {(grace_points-0.5)/0.1}")
                print(f"New max_attempts = {max_attempts}, attempt = {attempt}")
            else:
                # Reset the context
                pipeline = setup_fix_pipeline()


            try:
                # Attempt fix
                prev_project_state = current_project_state
                current_project_state = attempt_fix_iteration(
                    pipeline, current_project_state, test_case,
                    trace_dicts[test_case], trace_logs[test_case], 
                    error_category, attempt_dir
                )
                total_tokens_input += pipeline.get_total_input_tokens()
                total_tokens_output += pipeline.get_total_output_tokens()
            except Exception as e:
                print(f"EXCEPTION WHILE FIXING:\n\t{e}")
                file_utils.write_file(f"{new_project_root}/exception.txt", f"{e}")
                new_project_root = prev_project_root
                current_project_state = prev_project_state
                attempt += 1
                continue

            # Write updated state
            file_utils.write_project_state(current_project_state, new_project_root)

            # Try compilation
            print(f"COMPILING: {new_project_root}")
            compilable = compile_utils.try_compile(new_project_root, f"{attempt_dir}/std_streams")
        
            
            if not compilable:
                # Ideally we should add sanity check here to see if that fixes the issue.
                print("COMPILATION FAILED...")
                error_msg = file_utils.read_file(f"{attempt_dir}/std_streams/compile/stdout.txt").splitlines()
                truncated_msg = "\n".join(error_msg[-10:])
                print("----- COMPILE ERROR --------")
                print(truncated_msg)
                print("----------------------------")
                print(f"Reverting {new_project_root} -> {prev_project_root}")
                print(f"Next attempt: {attempt+1}")
                print("----------------------------")
                new_project_root = prev_project_root # handle_compilation_failure(new_project_root, prev_project_root)
                current_project_state = prev_project_state
                attempt += 1
                continue
                
            attempt += 1

    write_fix_diff_log(orig_results, orig_trace_logs, latest_results, latest_trace_logs, f"{test_name_dir}/fix_diff.json")

    with open(f"{test_name_dir}/token_usage_for_fixer.json", "w") as f:
        json.dump({"input":total_tokens_input, "output":total_tokens_output}, f, indent=4)

    return (pipeline, prev_project_root)

def write_fix_diff_log(orig_results, orig_trace_logs, new_results, new_trace_logs, out_file):

    diff_dict = {}
    for test_name in orig_results:

        
        if test_name not in new_results:
            diff_dict[test_name] = {"changed":True, "new":"DOES NOT EXIST!", "original":orig_results[test_name]}
            continue

        orig_error_category = f"{identify_error_category(orig_trace_logs[test_name]) if test_name in orig_trace_logs else None}"
        new_error_category = f"{identify_error_category(new_trace_logs[test_name]) if test_name in new_trace_logs else None}"

        old_result = orig_results[test_name]
        new_result = new_results[test_name]

        changed = old_result != new_result or (old_result == new_result and orig_error_category != new_error_category)
        diff_dict[test_name] = {
            "changed": changed, 
            "new": {
                "result":new_result, 
                "category":new_error_category
                }, 
            "original": {
                "result": old_result,
                "category": orig_error_category
                }
            }

    with open(out_file, "w") as f:
        json.dump(diff_dict, f, indent=4)

def oracle_fix_pchecker_errors(task, test_func_out, out_dir=None):
    test_name, _ = task
    pipeline, generated_dir = test_func_out
    # print(f"task = {task}")
    # print(f"test_func_out = {test_func_out}")
    # print(f"out_dir = {out_dir}")

    out_dir_test = f"{out_dir}/{test_name}"
    os.makedirs(out_dir_test, exist_ok=True)

    token_usage = pipeline.get_token_usage()
    with open(f'{out_dir_test}/conversation.json', 'w') as f:
        json.dump(pipeline.get_conversation(), f, cls=BytesEncoder, indent=4)

    with open(f'{out_dir_test}/token_usage.json', 'w') as f:
        json.dump(token_usage, f, cls=BytesEncoder, indent=4)

    results = {}

    compilable = compile_utils.try_compile(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
    results['compile'] = compilable
    if compilable:
        checker_out, *_ = try_pchecker(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
        results = {**results, **checker_out}

    return results

def test_dd2proj_replicated_with_pchecker(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    (pipeline, project_root) = test_dd2proj_replicated(task, model, temperature, n, heuristic, max_tokens, top_p, **kwargs)
    (pipeline2, project_root2) = test_fix_pchecker_errors(project_root)


def test_dd2proj_psrc(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):

    global SAVED_GLOBAL_STATE
    SAVED_GLOBAL_STATE = save_module_state(global_state)
    
    all_responses = {}
    
    _, dd_path, benchmark_dir = task
    dd_content = file_utils.read_file(dd_path)
    
    project_name_pattern = r'<title>(.*?)</title>'

    match = re.search(project_name_pattern, dd_content, re.IGNORECASE)
    if match:
        global_state.project_name = match.group(1).strip().replace(" ", "_")
    
    timestamp = global_state.current_time.strftime('%Y_%m_%d_%H_%M_%S')
    global_state.project_name_with_timestamp = f"{global_state.project_name}_{timestamp}"
    log_utils.move_recent_to_archive()
    file_utils.empty_file(global_state.full_log_path)
    file_utils.empty_file(global_state.code_diff_log_path)
    
    destination_path = ".tmp"
    if destination_path and destination_path.strip() != "" and file_utils.check_directory(destination_path.strip()):
        global_state.custom_dir_path = destination_path.strip()

    parent_abs_path = global_state.custom_dir_path or os.path.join(os.getcwd(), global_state.recent_dir_path)
    
    project_root = os.path.join(parent_abs_path, global_state.project_name_with_timestamp)

    mock_create_proj_files(project_root, benchmark_dir)

    pipeline = PromptingPipeline()
    system_prompt = file_utils.read_file(global_state.system_prompt_path)
    pipeline.add_system_prompt(system_prompt)

    # Get initial machine list
    text = instructions[LIST_OF_MACHINE_NAMES].format(userText=dd_content)
    pipeline.add_user_msg(text, [global_state.P_basics_path])
    pipeline.add_user_msg("These are the example P Programs ",[global_state.P_program_example_path])

    fixed_responses_dir = f"{benchmark_dir}/fixed_responses"
    psrc_dir_path = f"{benchmark_dir}/PSrc"
    # TODO: Simulate this call with hard coded answer for the benchmark using add_assistant_msg()
    machines_list = file_utils.read_file(f"{fixed_responses_dir}/machine_list.txt") # pipeline.invoke_llm(model, candidates=1, heuristic='random')
    pipeline.add_assistant_msg(machines_list)

    # Generate filenames
    pipeline.add_user_msg(instructions[LIST_OF_FILE_NAMES])
    response = file_utils.read_file(f"{fixed_responses_dir}/filenames_list.txt") # pipeline.invoke_llm(model, candidates=1, heuristic='random')
    
    
    global_state.filenames_map = extract_filenames(response)
    # Generate enums, types, and events
    pipeline.add_user_msg(instructions[ENUMS_TYPES_EVENTS])
    pipeline.add_documents_inline(get_context_files()["ENUMS_TYPES_EVENTS"], tag_surround)


    # response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
    mock_response = tag_surround("Enums_Types_Events.p", file_utils.read_file(f"{psrc_dir_path}/Enums_Types_Events.p"))
    pipeline.add_assistant_msg(mock_response)
    response = pipeline.get_last_response()
    log_filename, Pcode = extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, PSRC)
    
    file_abs_path = os.path.join(project_root, PSRC, log_filename)
    logger.info(f":blue[. . . filepath: {file_abs_path}]")
    
    if log_filename is not None and Pcode is not None:
        all_responses[log_filename] = Pcode

    step_no = 2

    def p_dirs_sort_func(item):
        dirname, _ = item
        dirname_priority_map = {PSRC: 3, PSPEC: 2, PTST: 1}
        return dirname_priority_map[dirname]

    for dirname, filenames in sorted(global_state.filenames_map.items(), key=p_dirs_sort_func):
        if dirname != PSRC:
            logger.info(f"Step {step_no}: Generating {dirname}")
            step_no += 1
            
        for filename in filenames:
            logger.info(f"Generating file {filename}.p")
            if dirname == PSRC and filename in machines_list:
                log_filename, Pcode = generate_machine_code(model, pipeline, instructions, filename, dirname)
            else:
                if dirname != PSRC:
                    log_filename, Pcode = generate_generic_file_mock(model, pipeline, instructions, filename, dirname, benchmark_dir)
                else:
                    log_filename, Pcode = generate_generic_file(model, pipeline, instructions, filename, dirname)


            log_file_full_path = os.path.join(project_root, dirname, log_filename)
            logger.info(f":blue[. . . filepath: {log_file_full_path}]")
            if log_filename is not None and Pcode is not None:
                all_responses[log_filename] = Pcode

        logger.info(f"Running the P compiler and analyzer on {dirname}...")
        num_iterations = 20 if dirname == PTST else 15
        
        if dirname == PSRC:
            compiler_analysis(model, pipeline, all_responses, num_iterations)
    
    # return all_responses
    return (pipeline, project_root)

def oracle_dd2proj_replicated(task, test_func_out, out_dir=None):
    global SAVED_GLOBAL_STATE
    dd_name, *_ = task
    pipeline, project_root = test_func_out
    out_dir_test = f"{out_dir}/{dd_name}"
    os.makedirs(out_dir_test, exist_ok=True)

    token_usage = pipeline.get_token_usage()

    generated_dir = f"{out_dir_test}/generated"
    subprocess.run(['cp', '-r', project_root, generated_dir])
    subprocess.run(['rm', '-rf', f"{generated_dir}/PGenerated"])
    subprocess.run(['cp', '-r', global_state.full_log_path, f"{out_dir_test}/full_log.txt"])
    subprocess.run(['cp', '-r', global_state.code_diff_log_path, f"{out_dir_test}/code_diff_log.txt"])
    # subprocess.run(['cp', '-r', global_state.communication_log_file, f"{out_dir_test}/comm_log.txt"])

    with open(f'{out_dir_test}/token_usage.json', 'w') as f:
        json.dump(token_usage, f, cls=BytesEncoder, indent=4)

    restore_module_state(global_state, SAVED_GLOBAL_STATE)

    results = {}
    compilable = compile_utils.try_compile(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
    results['compile'] = compilable
    if compilable:
        checker_out, *_ = try_pchecker(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
        results = {**results, **checker_out}

    return results

from utils.checker_utils import try_pchecker

def oracle_dd2psrc_correctness(task, test_func_out, out_dir=None):
    global SAVED_GLOBAL_STATE
    dd_name, *_ = task
    pipeline, project_root = test_func_out
    out_dir_test = f"{out_dir}/{dd_name}"
    os.makedirs(out_dir_test, exist_ok=True)

    token_usage = pipeline.get_token_usage()

    generated_dir = f"{out_dir_test}/generated"
    subprocess.run(['cp', '-r', project_root, generated_dir])
    subprocess.run(['rm', '-rf', f"{generated_dir}/PGenerated"])
    subprocess.run(['cp', '-r', global_state.full_log_path, f"{out_dir_test}/full_log.txt"])
    subprocess.run(['cp', '-r', global_state.code_diff_log_path, f"{out_dir_test}/code_diff_log.txt"])
    # subprocess.run(['cp', '-r', global_state.communication_log_file, f"{out_dir_test}/comm_log.txt"])

    with open(f'{out_dir_test}/token_usage.json', 'w') as f:
        json.dump(token_usage, f, cls=BytesEncoder, indent=4)

    restore_module_state(global_state, SAVED_GLOBAL_STATE)
    results = {}

    compilable = compile_utils.try_compile(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
    results['compile'] = compilable
    if compilable:
        checker_out, *_ = try_pchecker(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
        results = {**results, **checker_out}
        
    return results

# =======================================================================================


# EXAMPLE TEST COMMAND:
# python evaluate_chatbot.py --metric pass_at_k -k 1 -n 1 -t 1.0 --trials 2 --benchmark-dir resources/evaluation/p-model-benchmark/3_designdoc2pproj
@pytest.mark.parametrize("task", [("1_lightswitch", "resources/p-model-benchmark/3_designdoc2pproj/1_lightswitch.txt")])
def test_dd2proj_legacy(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
        import legacy.utils.chat_utils as legacy_chat_utils
        from legacy.utils import global_variables

        global SAVED_GLOBAL_STATE
        SAVED_GLOBAL_STATE = save_module_state(global_variables)

        global_variables.temperature = temperature
        global_variables.model_id = model
        
        dd_name, dd_path = task
        destination_path = ".tmp"
        if destination_path and destination_path.strip() != "" and file_utils.check_directory(destination_path.strip()):
            global_variables.custom_dir_path = destination_path.strip()
        
        design_doc_content = file_utils.read_file(dd_path)
        user_inp = "User uploaded Design Document: " + dd_name
        global_variables.chat_history.add_exchange("user", None, user_inp, None)

        legacy_chat_utils.generate_response(design_doc_content, MockContainer())
        # global_variables.custom_dir_path = None
        # global_variables.chat_history.clear_conversation()
        # return ".tmp/Light_Control_System_2025_06_28_00_41_29"
        return f"{destination_path}/{global_variables.project_name_with_timestamp}"


def oracle_dd2proj(task, pproj_path, out_dir=None):
    global SAVED_GLOBAL_STATE
    from legacy.utils import global_variables
    dd_name, _ = task
    out_dir_test = f"{out_dir}/{dd_name}"
    os.makedirs(out_dir_test, exist_ok=True)

    token_usage = {
        "cumulative": {
            **global_variables.model_metrics
        }
    }

    generated_dir = f"{out_dir_test}/generated"
    subprocess.run(['cp', '-r', pproj_path, generated_dir])
    subprocess.run(['rm', '-rf', f"{generated_dir}/PGenerated"])
    subprocess.run(['cp', '-r', global_variables.full_log_path, f"{out_dir_test}/full_log.txt"])
    subprocess.run(['cp', '-r', global_variables.code_diff_log_path, f"{out_dir_test}/code_diff_log.txt"])
    try:
        subprocess.run(['cp', '-r', global_variables.communication_log_file, f"{out_dir_test}/comm_log.txt"])
    except:
        pass

    with open(f'{out_dir_test}/token_usage.json', 'w') as f:
        json.dump(token_usage, f, cls=BytesEncoder, indent=4)

    restore_module_state(global_variables, SAVED_GLOBAL_STATE)

    compilable = compile_utils.try_compile(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
    return {'compile': compilable}

def oracle_dd2proj_current(task, pproj_path, out_dir=None):
    global SAVED_GLOBAL_STATE
    from utils import global_state
    dd_name, _ = task
    out_dir_test = f"{out_dir}/{dd_name}"
    os.makedirs(out_dir_test, exist_ok=True)

    token_usage = {
        "cumulative": {
            **global_state.model_metrics
        }
    }

    generated_dir = f"{out_dir_test}/generated"

    print(f"pproj_path = {pproj_path}")
    print(f"generated_dir = {generated_dir}")

    subprocess.run(['cp', '-r', pproj_path, generated_dir])
    subprocess.run(['rm', '-rf', f"{generated_dir}/PGenerated"])
    subprocess.run(['cp', '-r', global_state.full_log_path, f"{out_dir_test}/full_log.txt"])
    subprocess.run(['cp', '-r', global_state.code_diff_log_path, f"{out_dir_test}/code_diff_log.txt"])


    with open(f'{out_dir_test}/token_usage.json', 'w') as f:
        json.dump(token_usage, f, cls=BytesEncoder, indent=4)

    restore_module_state(global_state, SAVED_GLOBAL_STATE)

    compilable = compile_utils.try_compile(generated_dir, captured_streams_output_dir=f"{out_dir_test}/std_streams")
    return {'compile': compilable}

IGNORE_TESTS = [
    "1_lightSwitch", 
    "2_other", 
    # "1_basicMachineStructure",
    # "2_basicTypeDecl", 
    # "3_basicEventDecl",
    # "4_basicParameterizedStateEntry",
    # "5_basicCompleteSystem"
    ]

def taskgen_base(pdir): 
    prompts = {} # "<testname>": "<prompt>"

    # subdirs = [f.path for f in os.scandir(pdir) if f.is_dir()]
    # for subdir in subdirs:
    #     prompts = {**prompts, **construct_prompts_from_dir(subdir)}
    
    prompts = construct_prompts_from_dir(pdir)
    for t in IGNORE_TESTS:
        if t in prompts:
            del prompts[t]

    return prompts.items()


def create_base_old_pipeline():
    p_basics_file = 'resources/context_files/P_syntax_guide.txt'
    about_p_file = "resources/context_files/about_p.txt"
    system_prompt = file_utils.read_file(about_p_file)
    initial_msg = 'Read the attached P language basics guide for reference. You can refer to this document to understand P syntax and answer accordingly. Additional specific syntax guides will be provided as needed for each task.'
    
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg, documents = [p_basics_file])
    pipeline.add_assistant_msg('I understand. I will refer to the P language guides to provide accurate information about P syntax when answering questions.')
    return pipeline

def test_base_old(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    _, user_prompt = task

    p_basics_file = 'resources/context_files/modular/p_basics.txt'
    about_p_file = "resources/context_files/about_p.txt"
    p_basics_file_contents = file_utils.read_file(p_basics_file)
    system_prompt = file_utils.read_file(about_p_file)
    initial_msg = 'Read the attached P language basics guide for reference. You can refer to this document to understand P syntax and answer accordingly. Additional specific syntax guides will be provided as needed for each task.'
    
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg, documents = [p_basics_file])
    pipeline.add_assistant_msg('I understand. I will refer to the P language guides to provide accurate information about P syntax when answering questions.')
    pipeline.add_user_msg(f"{user_prompt}\n\n{p_basics_file_contents}")
    pipeline.invoke_llm(model=model, candidates=n, heuristic=heuristic, inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p})
    return pipeline

def test_base_all_docs(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    _, user_prompt = task

    p_basics_files = glob("resources/context_files/modular/*.txt")
    about_p_file = "resources/context_files/about_p.txt"
    system_prompt = file_utils.read_file(about_p_file)
    initial_msg = 'Read the attached P language basics guide for reference. You can refer to this document to understand P syntax and answer accordingly. Additional specific syntax guides will be provided as needed for each task.'
    
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)

    pipeline.add_documents_inline(
        p_basics_files,
        pre=f"{initial_msg}\n",
        post=f"\n\n{user_prompt}\n",
        preprocessing_function=lambda _,c: f"{c}\n")
    
    pipeline.invoke_llm(model=model, candidates=n, heuristic=heuristic, inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p})
    return pipeline

def test_base_few_shot(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    _, user_prompt = task

    p_few_shot = 'resources/context_files/modular-fewshot/p-fewshot-formatted.txt'
    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary."
    
    initial_msg = 'Here are some information relevant to P.'
    
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg, documents = [p_few_shot])
    pipeline.add_user_msg(f"{user_prompt}")
    pipeline.invoke_llm(model=model, candidates=n, heuristic=heuristic, inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p})
    return pipeline

def test_base_RAG2000_inline(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    from rag.load_rag_index import load_index, search_index

    _, user_prompt = task

    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary. Declare anything you wish to use."
    
    indices_root = "resources/rag/indices/2025-07-02-12-18-53/faiss_index_2000"
    index, chunks = load_index(RAG_MODEL_aMLML6v2, f"{indices_root}.faiss",f"{indices_root}.pkl")
    result = search_index(RAG_MODEL_aMLML6v2, index, user_prompt, chunks)
    retrieved_chunks = list(zip(*result))[0] 
    chunks_str = "\n".join(retrieved_chunks)
    initial_msg = f'Here is some information relevant to the query.\n{chunks_str}'

    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg)
    pipeline.add_user_msg(f"{user_prompt}")
    pipeline.invoke_llm(
            model=model, 
            candidates=n, 
            heuristic=heuristic, 
            inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p}
        )
    return pipeline
    

def test_base_RAG1000_inline(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    from rag.load_rag_index import load_index, search_index

    _, user_prompt = task

    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary. Declare anything you wish to use."
    
    indices_root = "resources/rag/indices/2025-07-02-11-15-36/faiss_index_1000"
    index, chunks = load_index(RAG_MODEL_aMLML6v2, f"{indices_root}.faiss",f"{indices_root}.pkl")
    result = search_index(RAG_MODEL_aMLML6v2, index, user_prompt, chunks)
    retrieved_chunks = list(zip(*result))[0] 
    chunks_str = "\n".join(retrieved_chunks)
    initial_msg = f'Here is some information relevant to the query.\n{chunks_str}'

    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg)
    pipeline.add_user_msg(f"{user_prompt}")
    pipeline.invoke_llm(
            model=model, 
            candidates=n, 
            heuristic=heuristic, 
            inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p}
        )
    return pipeline

def test_base_RAG2000_inline_fewshot(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    from rag.load_rag_index import load_index, search_index

    _, user_prompt = task

    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary. Declare anything you wish to use."
    
    indices_root = "resources/rag/aMLML6v2/fewshot-only/2025-07-03-14-29-32/faiss_index_2000"
    index, chunks = load_index(RAG_MODEL_aMLML6v2, f"{indices_root}.faiss",f"{indices_root}.pkl")
    result = search_index(RAG_MODEL_aMLML6v2, index, user_prompt, chunks)
    retrieved_chunks = list(zip(*result))[0] 
    chunks_str = "\n".join(retrieved_chunks)
    initial_msg = f'Here is some information relevant to the query.\n{chunks_str}'

    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg)
    pipeline.add_user_msg(f"{user_prompt}")
    pipeline.invoke_llm(
            model=model, 
            candidates=n, 
            heuristic=heuristic, 
            inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p}
        )
    return pipeline

def test_base_RAG2000_inline_aMLML12v2(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    from rag.load_rag_index import load_index, search_index

    _, user_prompt = task

    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary. Declare anything you wish to use."
    
    indices_root = "resources/rag/aMLML12v2/2025-07-03-13-15-34/faiss_index_2000"
    index, chunks = load_index(RAG_MODEL_aMLML12v2, f"{indices_root}.faiss",f"{indices_root}.pkl")
    result = search_index(RAG_MODEL_aMLML12v2, index, user_prompt, chunks)
    retrieved_chunks = list(zip(*result))[0] 
    chunks_str = "\n".join(retrieved_chunks)
    initial_msg = f'Here is some information relevant to the query.\n{chunks_str}'

    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg)
    pipeline.add_user_msg(f"{user_prompt}")
    pipeline.invoke_llm(
            model=model, 
            candidates=n, 
            heuristic=heuristic, 
            inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p}
        )
    return pipeline


def test_base_RAG2000_asdoc(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    from rag.load_rag_index import load_index, search_index

    _, user_prompt = task

    chunks_file = "/tmp/rag_chunks.txt"
    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary. Declare anything you wish to use."
    
    indices_root = "resources/rag/indices/2025-07-02-12-18-53/faiss_index_2000"
    index, chunks = load_index(RAG_MODEL_aMLML6v2, f"{indices_root}.faiss",f"{indices_root}.pkl")
    result = search_index(RAG_MODEL_aMLML6v2, index, user_prompt, chunks)
    retrieved_chunks = list(zip(*result))[0] 
    chunks_str = "\n".join(retrieved_chunks)
    initial_msg = f'Attached is some information about p.'

    with open(chunks_file, 'w') as f:
        f.write(chunks_str)

    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(f"{initial_msg}\n{user_prompt}", documents=[chunks_file])
    pipeline.invoke_llm(
            model=model, 
            candidates=n, 
            heuristic=heuristic, 
            inference_config={"maxTokens": max_tokens, "temperature": temperature, "topP": top_p}
        )
    return pipeline


def extract_code(llm_output):
    try:
        s = llm_output.split("```")[1]
        if s.startswith("p"):
            s = s[1:]
    except:
        s = llm_output.strip('`')
    return s

def logger_generic(task, pipeline, out_dir):
    test_name, _ = task
    print(f"[PASS@K] RUNNING ORACLE FOR TEST {test_name}")

    out_dir_test = f"{out_dir}/{test_name}"
    os.makedirs(out_dir_test, exist_ok=True)

    with open(f'{out_dir_test}/conversation.json', 'w') as f:
        json.dump(pipeline.get_conversation(), f, cls=BytesEncoder, indent=4)

    with open(f'{out_dir_test}/token_usage.json', 'w') as f:
        json.dump(pipeline.get_token_usage(), f, cls=BytesEncoder, indent=4)
    
    with open(f'{out_dir_test}/system_prompt.txt', 'w') as f:
        f.write(f"{pipeline.get_system_prompt()}")

    return out_dir_test


def oracle_base(task, pipeline, out_dir=None):
    llm_output = pipeline.get_last_response()

    out_dir_test = logger_generic(task, pipeline, out_dir)
    p_file = f'{out_dir_test}/generated.p'

    final_code = extract_code(llm_output)
    if not final_code:
        with open(f'{out_dir_test}/failed_model_call.txt', 'w') as f:
            f.write('model_call_failed')
        return False
    
    with open(p_file, 'w') as pfile:
        pfile.write(final_code)

    is_compilable = compile_utils.try_compile(p_file, captured_streams_output_dir=f"{out_dir_test}/std_streams")
    return {'compile': is_compilable}

class BytesEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, bytes):
            return f"{obj}"
        
        return super().default(obj)

def construct_prompt_from_pproj(pdir):
    return "TODO: Process project folder to create prompt."

def construct_prompts_from_pprojs(pdir):
    return {d.name:construct_prompt_from_pproj(d) for d in os.scandir(pdir) if d.is_dir()}

def construct_prompt_from_pfile(f): 
    with open(f.path, 'r') as prompt_file:
        prompt = prompt_file.read()
    
    return prompt

def strip_ext(f):
    return f.rsplit('.', 1)[0]

def construct_prompts_from_pfiles(pdir):
    def make_test_name(f):
        return strip_ext(f.name)

    return {make_test_name(f):construct_prompt_from_pfile(f) for f in os.scandir(pdir) if is_prompt_file(f)}

def is_prompt_file(f):
    ret = f.name.endswith(".prompt")
    return ret

def has_prompt_files(pdir):
    return any([is_prompt_file(f) for f in os.scandir(pdir)])

def construct_prompts_from_dir(pdir):
    if has_prompt_files(pdir):
        return construct_prompts_from_pfiles(pdir)
    else:
        return construct_prompts_from_pprojs(pdir)


def test_singleshot_designdoc_to_pcode(doc_list):
    """
    Test converting a design document to P code using the single-shot prompt.
    Uses singleshot_prompt.txt to generate complete P code in one LLM call.
    
    Args:
        doc_list: Fixture providing list of P language documentation files
    """
    # Get project root path
    logger.info(f"args: {doc_list}")
 
    project_root = Path(__file__).parent.parent.parent
    
    pipeline = PromptingPipeline()
    
    # Add system prompt
    system_prompt = file_utils.read_file(project_root / "resources/context_files/singleshot_prompt.txt")
    pipeline.add_system_prompt(system_prompt)
    
    # Add P language docs
    pipeline.add_text("Here are the P language documentation files for reference:")
    pipeline.add_documents_inline(doc_list, lambda fname, contents: tag_surround(fname, contents))
    
    # Add design doc
    pipeline.add_text("Here is the design document to implement:")
    designdoc_content = file_utils.read_file(project_root / "resources/p-model-benchmark/3_designdoc2pproj/1_lightswitch.txt")
    pipeline.add_text(designdoc_content)
    
    # Add generation instruction
    pipeline.add_text("Generate the complete P code implementation following the structure specified in the system prompt.")
    logger.info("Going to call it")
    # Generate P code
    logger.info("Invoking LLM...")
    response = pipeline.invoke_llm(model=CLAUDE_3_7, candidates=1, heuristic='random')
    
    # Log the response
    logger.info("LLM Response received:")
    logger.info("-" * 80)
    logger.info(response)
    logger.info("-" * 80)
    
    # Basic validation that response contains key P elements
    assert "machine" in response, "Generated code should contain state machines"
    assert "event" in response, "Generated code should contain events"
    assert "spec" in response, "Generated code should contain specifications"
    
    # Extract components using tags
    components = {}
    for tag in ["project_structure", "enums_and_types", "events", "machines", 
                "monitors", "module_structure", "spec_files", "test_files", 
                "file_organization"]:
        start_tag = f"<{tag}>"
        end_tag = f"</{tag}>"
        if start_tag in response and end_tag in response:
            start = response.find(start_tag) + len(start_tag)
            end = response.find(end_tag)
            components[tag] = response[start:end].strip()
            logger.info(f"\n{tag}:\n{components[tag]}")
    
    # Write generated code to files
    written_files = write_generated_p_code(components)
    logger.info(f"Written files: {written_files}")
    
    # Verify files were created
    assert len(written_files) > 0, "No files were generated"
    for filepath in written_files:
        assert os.path.exists(filepath), f"File not created: {filepath}"
    
    # Verify response contains required elements
    assert "machine" in response, "Generated code should contain state machines"
    assert "event" in response, "Generated code should contain events"
    assert "spec" in response, "Generated code should contain specifications"

def write_generated_p_code(components):
    """
    Write the generated P code components to files in a generated_code directory.
    
    Args:
        components: Dictionary containing the tagged components from LLM response
    Returns:
        list: Paths of written files
    """
    written_files = []
    
    # Extract project name from pproj_config
    project_name = None
    if 'pproj_config' in components:
        config = components['pproj_config']
        if 'ProjectName:' in config:
            project_name = config.split('ProjectName:', 1)[1].split('\n')[0].strip().strip('{}')
    
    # Create base directory structure in project root
    project_root = Path(__file__).parent.parent.parent
    base_dir = os.path.join(str(project_root), "generated_code")
    if project_name:
        base_dir = os.path.join(base_dir, project_name)
    
    # Create directories based on project_structure from LLM response
    if 'project_structure' in components:
        for line in components['project_structure'].split('\n'):
            if line.strip():
                dir_path = os.path.join(base_dir, line.strip().rstrip('/'))
                os.makedirs(dir_path, exist_ok=True)
                logger.info(f"Created directory: {dir_path}")
    
    # Write .pproj file
    if 'pproj_config' in components:
        pproj_path = os.path.join(base_dir, f"{project_name}.pproj" if project_name else "Project.pproj")
        pproj_content = f"""<?xml version="1.0" encoding="utf-8"?>
<Project>
  <ProjectName>{project_name if project_name else "Project"}</ProjectName>
  <InputFiles>
    <PFile>./PSrc</PFile>
    <PFile>./PSpec</PFile>
    <PFile>./PTst</PFile>
  </InputFiles>
  <OutputDir>./PGenerated</OutputDir>
</Project>"""
        with open(pproj_path, 'w') as f:
            f.write(pproj_content)
        written_files.append(pproj_path)
        logger.info(f"Wrote .pproj file to: {pproj_path}")
 
    # Parse file organization from LLM response
    file_mapping = {}
    if 'file_organization' in components:
        for line in components['file_organization'].split('\n'):
            if ':' in line:
                filename, content_type = line.split(':', 1)
                file_mapping[content_type.strip()] = filename.strip()
 
    # Write enums and types
    if 'enums_and_types' in components:
        # Get filename from file_mapping or use default, ensuring no prefix dashes
        filename = next((f.strip('-').strip() for f, t in file_mapping.items() if 'type' in t.lower()), 'Types.p')
        filepath = os.path.join(base_dir, "PSrc", filename)
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
        with open(filepath, 'w') as f:
            f.write(components['enums_and_types'])
        written_files.append(filepath)
        logger.info(f"Wrote enums and types to: {filepath}")
 
    # Write events
    if 'events' in components:
        # Get filename from file_mapping or use default, ensuring no prefix dashes
        filename = next((f.strip('-').strip() for f, t in file_mapping.items() if 'event' in t.lower()), 'Events.p')
        filepath = os.path.join(base_dir, "PSrc", filename)
        os.makedirs(os.path.dirname(filepath), exist_ok=True)
        with open(filepath, 'w') as f:
            f.write(components['events'])
        written_files.append(filepath)
        logger.info(f"Wrote events to: {filepath}")
 
    # Write machine files
    if 'machines' in components:
        for machine in components['machines'].split('<machine>'):
            if '</machine>' in machine:
                machine_content = machine.split('</machine>')[0].strip()
                if machine_content:
                    # Try to extract machine name from content
                    machine_name = None
                    if 'machine' in machine_content.lower():
                        try:
                            machine_name = machine_content.split('machine', 1)[1].split()[0].strip()
                        except:
                            pass
                    
                    # If no name found in content, look in file_mapping
                    if not machine_name:
                        machine_name = next((f.replace('.p', '').strip('-').strip() for f, t in file_mapping.items() 
                                          if 'machine' in t.lower()), 'Machine')
                    
                    filepath = os.path.join(base_dir, "PSrc", f"{machine_name}.p")
                    os.makedirs(os.path.dirname(filepath), exist_ok=True)
                    with open(filepath, 'w') as f:
                        f.write(machine_content)
                    written_files.append(filepath)
                    logger.info(f"Wrote machine to: {filepath}")
    
    # Write spec files
    if 'spec_files' in components:
        for spec in components['spec_files'].split('<spec_file'):
            if 'name=' in spec and '</spec_file>' in spec:
                name = spec.split('name="')[1].split('"')[0].strip('-').strip()
                content = spec.split('>')[1].split('</spec_file>')[0].strip()
                # Use name directly from LLM response, ensuring no prefix dashes
                filepath = os.path.join(base_dir, "PSpec", name)
                os.makedirs(os.path.dirname(filepath), exist_ok=True)
                with open(filepath, 'w') as f:
                    f.write(content)
                written_files.append(filepath)
                logger.info(f"Wrote spec file to: {filepath}")
    
    # Write test files
    if 'test_files' in components:
        for test in components['test_files'].split('<test_file'):
            if 'name=' in test and '</test_file>' in test:
                name = test.split('name="')[1].split('"')[0].strip('-').strip()
                content = test.split('>')[1].split('</test_file>')[0].strip()
                # Use name directly from LLM response, ensuring no prefix dashes
                filepath = os.path.join(base_dir, "PTst", name)
                os.makedirs(os.path.dirname(filepath), exist_ok=True)
                with open(filepath, 'w') as f:
                    f.write(content)
                written_files.append(filepath)
                logger.info(f"Wrote test file to: {filepath}")
    
    return written_files

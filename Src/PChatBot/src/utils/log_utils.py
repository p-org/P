from utils import file_utils
from utils import global_state as globals
import os, logging, difflib
from io import StringIO

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")

def log_Pcode(Pcode, parent_dirname, dirname, filename):
    """
    Logs the provided P code to a specified file within a directory structure.
    """
    folder_path = globals.custom_dir_path or globals.recent_dir_path
    file_path = os.path.join(folder_path, parent_dirname, dirname, filename)
    file_utils.write_file(file_path, Pcode)


def log_Pcode_to_file(Pcode, file_path):
    """
    Logs the provided P code to the specified file path.
    """
    if os.path.exists(file_path):
        logger.info(f"File doesnt exists to write {file_path} ")
    file_utils.write_file(file_path, Pcode)


def move_recent_to_archive():
    """
    Moves the recent directory content to the archive base directory.
    """
    file_utils.ensure_dir_exists(globals.recent_dir_path)
    if not file_utils.is_dir_empty(globals.recent_dir_path):
        file_utils.ensure_dir_exists(globals.archive_base_dir_path)
        file_utils.move_dir_contents(globals.recent_dir_path, globals.archive_base_dir_path)


def create_pproj_file(parent_dirname):
    """
    Creates a .pproj file with the parent_dirname name in the specified directory.
    """
    pproj_content = file_utils.read_file(globals.pproj_template_path).format(project_name=globals.project_name_with_timestamp)
    parent_dir = globals.custom_dir_path or globals.recent_dir_path
    file_path = os.path.join(parent_dir, parent_dirname, f"{parent_dirname}.pproj")
    file_utils.write_file(file_path, pproj_content)


def log_llmresponse(llmresponse):
    """
    Logs the LLM response to a file within the logs directory.
    """
    file_utils.append_file(globals.full_log_path, llmresponse)


def log_code_diff(original_code, modified_code, header_message):
    """
    Generate and log the diff between original and modified code.

    Args:
    original_code (str): Original P code content
    modified_code (str): Modified P code content after fixes
    """
    original_lines = original_code.splitlines()
    modified_lines = modified_code.splitlines()

    # Generate diff
    diff = difflib.unified_diff(original_lines, modified_lines, lineterm='')

    # Use StringIO for efficient string concatenation
    diff_output = StringIO()
    diff_output.write(f"{header_message}:\n")
    diff_output.write('\n'.join(diff))

    # Log the entire diff at once
    file_utils.append_file(globals.code_diff_log_path, diff_output.getvalue())






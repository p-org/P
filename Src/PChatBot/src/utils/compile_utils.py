import logging, re, os, subprocess, difflib
from utils import global_state as globals
from utils import file_utils, log_utils, generate_p_code, regex_utils, global_state
from collections import namedtuple
from pathlib import Path
from datetime import datetime

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")

LLMCorrectionInstruction = namedtuple('LLMCorrectionInstruction', ['correction_instruction', 'file_path', 'file_content'])
ERROR_FORMAT_PARAMS = {}
SPECIFIC_ERROR_DICT = file_utils.read_json_file(globals.specific_errors_list_path)
GENERAL_ERROR_DICT = file_utils.read_json_file(globals.general_errors_list_path)

def run_manual_syntax_validation(P_code):
    """
    Performs manual syntax validation and modification on the input code.
    This function processes the input code line by line, handling the following cases:
    1. Removes lines that initializes empty braces
    2. Removes lines that incorrectly initializes an empty collection 
    3. Fixes lines with '!in'
    4. Fixes single field tuples by adding a comma
    5. Ensures the parentheses are empty during machine instantiation.

    Logs any changes to the full_log.txt file.
    """
    final_P_code = []
    try:
        logger.info("Starting manual syntax validation…")
         # Parsing line-by-line
        for line in P_code.splitlines():
            if line.strip().startswith('//'):
                # Skip comment lines
                final_P_code.append(line)
                continue
            elif '= {}' in line or '= []' in line:
                logger.info(f"Removed line: {line}")
            elif ('= map' in line or '= set' in line or '= seq' in line) and "type" not in line:
                logger.info(f"Removed line: {line}")
            elif '!in' in line:
                replaced_code = regex_utils.replace_not_in(line)
                logger.info(f"Replaced !in in line: {line} with {replaced_code}")
                final_P_code.append(replaced_code)
            elif 'this.' in line:
                replaced_code = regex_utils.remove_this_dot(line)
                logger.info(f"Removed this. in line: {line}")
                final_P_code.append(replaced_code)
            elif 'const' in line:
                replaced_code = line.replace("const ", "var ")
                logger.info(f"Replaced const in line: {line} with {replaced_code}")
            elif 'ignore' in line:
                replaced_code = regex_utils.replace_on_ignore(line)
                if replaced_code != line:
                    logger.info(f"Replaced line: {line} with {replaced_code}")
                final_P_code.append(replaced_code)
            else:
                # Add comma to single-field tuples
                replaced_code = regex_utils.add_comma_to_single_field_tuple(line)
                if replaced_code != line:
                    logger.info(f"Added comma to single field tuple in line: {line}")
                final_P_code.append(replaced_code)
        
        final_P_code = '\n'.join(final_P_code)
        if final_P_code != P_code:
            log_utils.log_code_diff(P_code, final_P_code, "After manual syntax validation of the P code")
        
        return final_P_code

    except Exception as e:
        logger.error("Manual Syntax Validation Failed: " + str(e))


def compile_Pcode(P_project_path):
    """
    Attempts to compile the provided P code.

    Parameters:
    input_path (str): The path to the P project to be compiled.

    Returns:
    tuple: A tuple containing:
        - a boolean indicating success or failure of the compilation.
        - a string containing the error message if the compilation fails, empty string otherwise.
    """
    try:
        logger.info("Attempting to compile the generated P code…")
        logger.info(f"P project path: {P_project_path}")
        original_path = os.getcwd()
        os.chdir(P_project_path)
        
        command = ["p",  "compile"]
        result = subprocess.run(command, shell=False, capture_output=True, text=True)
        if result.stderr != "":
            raise ValueError('There was an issue with compiling the P project code. ', result.stderr)
        logger.info("Result of p compile: " + result.stdout)
        
        os.chdir(original_path)
        logger.info("Changed current dir path to: " + os.getcwd())

        if "succeeded" not in result.stdout:
            return False, result.stdout
        return True, ""
    
    except Exception as e:
        logger.info("An unexpected error occured when compiling the P project. " + str(e))


def get_correction_instruction(P_filenames_dict, compilation_result):
    """
    Generates a correction instruction for a given compilation error in P code.

    Parameters:
    P_filenames_dict (dict): A dictionary mapping P filenames to their corresponding file paths.
    compilation_result (str): The compilation result string that contains the error details.

    Returns the generated correction instruction along with the file path and file content.
    """
    # 1. Obtain the error message
    filename, line_number, col_number, error_message = regex_utils.parse_compiler_error(compilation_result)
    error_message = error_message.strip()
    log_utils.log_llmresponse(f"Error Message: File {filename} at Line Number {line_number}.\n{error_message}\n")

    # 2. Obtain the content of the error file
    target_file_path = P_filenames_dict[filename]
    target_file_content = file_utils.read_file(target_file_path)
    target_file_line = file_utils.read_file_line(target_file_path, int(line_number))

    correction_instruction = f"In the file {filename}, fix this: {target_file_line.strip()}\n"

    # 3. Retrieve the mapped instructions
    if error_message in SPECIFIC_ERROR_DICT:
        correction_instruction += SPECIFIC_ERROR_DICT.get(error_message).format(**ERROR_FORMAT_PARAMS)
    else:
        correction_instruction += get_instructions_for_general_errors(error_message)

    return LLMCorrectionInstruction(correction_instruction, target_file_path, target_file_content)


def get_instructions_for_general_errors(error_message):
    """
    Handles general errors by checking the error message against known general error patterns.
    Constructs a message to send back to the LLM for fixing the error.
    """
    for key, correction_instruction in GENERAL_ERROR_DICT.items():
        if key in error_message:
            if "{name}" in correction_instruction:
                name = regex_utils.extract_name_from_error(error_message)
                return correction_instruction.format(name=name)
            return correction_instruction
        
        elif '.+' in key:
            # Use regex matching for patterns containing '.+'
            if re.search(key, error_message):
                if "{name}" in correction_instruction:
                    name = regex_utils.extract_name_from_error(error_message)
                    return correction_instruction.format(name=name)
                return correction_instruction
    
    # If the error message isn't mapped, return the error_message itself.
    return error_message

def parse_compile_error(error_msg):
    pattern = r"\[(.*?)\] parse error: line (\d+):(\d+)"
    match = re.search(pattern, error_msg)
    if not match:
        pattern = r"\[(.*?)\:(\d+):(\d+)\]"
        match = re.search(pattern, error_msg)

    if match:
        return match.group(1), match.group(2), match.group(3) 
    return "", "", ""


def try_compile(ppath, captured_streams_output_dir):

    subprocess.run(["rm", "-rf", f"{ppath}/PGenerated"])

    p = Path(ppath)
    flags = ['-pf', ppath, "-o", str(p.parent)] if p.is_file() else []

    final_cmd_arr = ['p', 'compile', *flags]
    result = subprocess.run(final_cmd_arr, capture_output=True, cwd=ppath if not p.is_file() else None)

    out_dir = f"{captured_streams_output_dir}/compile"
    os.makedirs(out_dir, exist_ok=True)
    file_utils.write_output_streams(result, out_dir)
    return result.returncode == 0

def try_compile_project_state(project_state, captured_streams_output_dir=None):
    timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
    tmp_project_dir = f"/tmp/compile-utils/{timestamp}"
    file_utils.write_project_state(project_state, tmp_project_dir)
    passed = try_compile(tmp_project_dir, f"{tmp_project_dir}/std_streams")
    stdout = file_utils.read_file(f"{tmp_project_dir}/std_streams/compile/stdout.txt")
    return passed, tmp_project_dir, stdout
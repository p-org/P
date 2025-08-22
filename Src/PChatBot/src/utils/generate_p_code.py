import logging
import time
import traceback
import re
from botocore.exceptions import ClientError
import os
from utils import file_utils, regex_utils, log_utils, compile_utils, global_state
from core.pipelining import prompting_pipeline
import json

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(levelname)s: %(message)s")


# File handler
file_handler = logging.FileHandler(os.path.join(global_state.PROJECT_ROOT, "pchatbot_debug.log"), mode='w')  # 'w' to overwrite
file_handler.setFormatter(logging.Formatter("%(levelname)s: %(message)s"))

# Console handler
console_handler = logging.StreamHandler()
console_handler.setFormatter(logging.Formatter("%(levelname)s: %(message)s"))

# Add handlers to logger
logger.addHandler(file_handler)
logger.addHandler(console_handler)

LIST_OF_MACHINE_NAMES = 'list_of_machine_names'
LIST_OF_FILE_NAMES = 'list_of_file_names'
ENUMS_TYPES_EVENTS = 'enums_types_events'
MACHINE = 'machine'
MACHINE_STRUCTURE = "MACHINE_STRUCTURE"
PROJECT_STRUCTURE="PROJECT_STRUCTURE"
SANITY_CHECK = 'sanity_check'

PSRC = 'PSrc'
PSPEC = 'PSpec'
PTST = 'PTst'

def read_instructions():
    return {
        LIST_OF_MACHINE_NAMES: file_utils.read_file(global_state.initial_instructions_path),
        LIST_OF_FILE_NAMES: file_utils.read_file(global_state.generate_filenames_instruction_path),
        ENUMS_TYPES_EVENTS: file_utils.read_file(global_state.generate_enums_types_events_instruction_path),
        MACHINE_STRUCTURE: file_utils.read_file(global_state.generate_machine_structure_path),
        PROJECT_STRUCTURE: file_utils.read_file(global_state.generate_project_structure_path),
        SANITY_CHECK: file_utils.read_file(global_state.sanity_check_instructions_path),
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
            "P_PROGRAM_STRUCTURE": [
                global_state.P_PROGRAM_STRUCTURE_GUIDE
            ],
            "MACHINE_STRUCTURE": [
                global_state.P_MACHINES_GUIDE
            ],
            PSRC: [
                global_state.P_TYPES_GUIDE,
                global_state.P_STATEMENTS_GUIDE,
                global_state.P_MODULE_SYSTEM_GUIDE
            ],
            PSPEC: [
                global_state.P_TYPES_GUIDE,
                global_state.P_SPEC_MONITORS_GUIDE
            ],
            PTST: [
                global_state.P_TYPES_GUIDE,
                global_state.P_TEST_CASES_GUIDE
            ],
            "COMPILE" :[
                global_state.P_SYNTAX_SUMMARY,
                global_state.P_COMPILER_GUIDE
            ]
        }

def entry_point(design_doc_content, backend_status):
    """
    Entry point function that processes user input and generates response.
    """
    start_time = time.time()
    set_project_name_from_design_doc(design_doc_content)
    log_utils.move_recent_to_archive()
    file_utils.empty_file(global_state.full_log_path)
    file_utils.empty_file(global_state.code_diff_log_path)
    create_proj_files(backend_status)
    all_responses = generate_p_code(design_doc_content, backend_status)
    global_state.total_runtime = round(time.time() - start_time, 3)
    return all_responses


def create_proj_files(backend_status):
    
    # Create project structure with folders and pproj file
    from utils.project_structure_utils import setup_project_structure
    parent_abs_path = global_state.custom_dir_path or os.path.join(os.getcwd(), global_state.recent_dir_path)
    project_root = os.path.join(parent_abs_path, global_state.project_name_with_timestamp)
    backend_status.write("Step 0: Creating project structure...")
    setup_project_structure(project_root, global_state.project_name)
    backend_status.write(f":white_check_mark: Project structure created at: {project_root}")
    

def generate_p_code(design_doc_content, backend_status):
    try: 
        """
        Invokes the prompting_pipeline that calls llm to generate conversation responses and related code files.

        Returns:
        all_responses (dict): A dictionary where keys are log filenames and values are generated P code.
        """
        backend_status.write("Here in Generating P Code ")
        system_prompt = file_utils.read_file(global_state.system_prompt_path)
        all_responses = {}
        parent_abs_path = global_state.custom_dir_path or os.path.join(os.getcwd(), global_state.recent_dir_path)
        backend_status.write(f"Parent Abs path : {parent_abs_path} ")

        try:
            machines_list = generate_machine_names(system_prompt,design_doc_content, backend_status)
            # Generate filenames
            generate_filenames(system_prompt,design_doc_content,machines_list, backend_status, True)
            # Generate enums, types, and events
            response = generate_enum_types_events(system_prompt,design_doc_content,machines_list, backend_status)
            backend_status.write("Running sanity check on response...")
            # Run sanity check on the generated code
            fixed_response = sanity_check(system_prompt, response, all_responses, machines_list, backend_status)

            log_filename, Pcode = extract_validate_and_log_Pcode(fixed_response,
                                                            global_state.project_name_with_timestamp, PSRC)
            if log_filename is not None and Pcode is not None:
                file_abs_path = os.path.join(parent_abs_path, global_state.project_name_with_timestamp, PSRC, log_filename)
                backend_status.write(f":blue[. . . filepath: {file_abs_path}]")            
                all_responses[log_filename] = Pcode

            # Generate P models, specs, and tests
            step_no = 2

            for dirname, filenames in global_state.filenames_map.items():
                if dirname != PSRC:
                    backend_status.write(f"Step {step_no}: Generating {dirname}")
                    step_no += 1
                    
                for filename in filenames:
                    backend_status.write(f"Generating file {filename}.p")
                    if dirname == PSRC and filename in machines_list:
                        log_filename, Pcode = generate_machine_code(system_prompt, design_doc_content, machines_list, filename, backend_status,
                                                                    dirname, all_responses)
                    else:
                        log_filename, Pcode = generate_generic_file(system_prompt, design_doc_content, machines_list, filename, backend_status,
                                                                    dirname, all_responses)

                    log_file_full_path = os.path.join(parent_abs_path, global_state.project_name_with_timestamp, dirname, log_filename)
                    backend_status.write(f":blue[. . . filepath: {log_file_full_path}]")
                    if log_filename is not None and Pcode is not None:
                        all_responses[log_filename] = Pcode

                backend_status.write(f"Running the P compiler and analyzer on {dirname}...")
                num_iterations = 15
                compiler_analysis(system_prompt,all_responses, machines_list,num_iterations, backend_status)
            return all_responses

        except ClientError as err:
            user_message = err.response['Error']['Message']
            logger.error("A client error occurred: %s", user_message)
        except Exception as e:
            logger.error(e)
            traceback.print_exc()
    except FileNotFoundError as fe : 
        logger.error(fe)
        traceback.print_exc()

def generate_machine_names(system_prompt,design_doc_content,backend_status):
     # Initialize the Pipeline
    pipeline = prompting_pipeline.PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    # Get initial machine list
    instructions = read_instructions()[LIST_OF_MACHINE_NAMES].format(userText=design_doc_content)
    pipeline.add_user_msg(instructions, [global_state.P_basics_path])
    pipeline.add_documents_inline(get_context_files()["P_PROGRAM_STRUCTURE"], tag_surround)
    response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
    log_token_count(pipeline, backend_status, "Generate Machine names")
    return response

def generate_filenames(system_prompt,design_doc_content,machines_list, backend_status, design_to_code_mode = False):
    pipeline = prompting_pipeline.PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(design_doc_content)
    pipeline.add_user_msg(machines_list)
    pipeline.add_documents_inline(get_context_files()["P_PROGRAM_STRUCTURE"], tag_surround)
    pipeline.add_user_msg(read_instructions()[LIST_OF_FILE_NAMES], [global_state.P_basics_path])
    file_names = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
    log_token_count(pipeline, backend_status, "Generate Filenames")
    global_state.filenames_map = extract_filenames(file_names)
    return file_names

def generate_enum_types_events(system_prompt,design_doc_content,machines_list,backend_status):
    pipeline = prompting_pipeline.PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)

    pipeline.add_user_msg(machines_list)
    pipeline.add_documents_inline(get_context_files()["ENUMS_TYPES_EVENTS"], tag_surround)
    pipeline.add_user_msg(f"All of the above are for your reference and context")

    backend_status.write("Step 1: Generating PSrc")
    backend_status.write("Generating a P file for P enums, types, and events...")

    pipeline.add_user_msg(read_instructions()[ENUMS_TYPES_EVENTS], [global_state.P_basics_path])
    pipeline.add_user_msg(f"This is the Design Document for which I want you to generate the code for : /n {design_doc_content}")

    response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
    log_token_count(pipeline, backend_status, "Generating enum_type_events.p")
    return response

def tag_surround(tagname, contents):
    return f"<{os.path.basename(tagname)}>\n{contents}\n</{os.path.basename(tagname)}>"


def log_token_usage(token_usage, backend_status):
    """Log token usage metrics to the backend status."""
    backend_status.write(f"Token usage - Stage: Input {token_usage['inputTokens']}, Output {token_usage['outputTokens']} | "
                         f"Cumulative: Input {global_state.model_metrics['inputTokens']}, Output {global_state.model_metrics['outputTokens']}")


def generate_machine_code(system_prompt, design_doc_content,machines_list, filename, backend_status, dirname, all_responses):
    """Generate machine code using either two-stage or single-stage process."""
    # Stage 1: Generate structure
    backend_status.write(f"  . . . Stage 1: Generating structure for {filename}.p")
    pipeline = prompting_pipeline.PromptingPipeline()
    pipeline.add_system_prompt(read_instructions()['MACHINE_STRUCTURE'].format(machineName=filename))
    pipeline.add_documents_inline(get_context_files()["MACHINE_STRUCTURE"], tag_surround)
    pipeline.add_user_msg("P_basics_file",[global_state.P_basics_path])
    pipeline.add_user_msg(f"This is the Design Document for which I want you to generate the code for : /n {design_doc_content}")
    pipeline.add_user_msg(f"Other relevant files of this P Program that may contain declarations: {json.dumps(all_responses)}")

    stage1_response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')

    structure_pattern = r'<structure>(.*?)</structure>'
    match = re.search(structure_pattern, stage1_response, re.DOTALL)
    if match:
        # Two-stage generation
        machine_structure = match.group(1).strip()
        backend_status.write(f"  . . . Stage 2: Implementing function bodies for {filename}.p")
        # Format instruction text first, then combine with machine structure
        instruction_text = read_instructions()[MACHINE].format(machineName=filename)
        # Replace any curly braces in machine structure with escaped versions
        pipeline.add_user_msg(f"{instruction_text}\n\nHere is the starting structure:\n\n"+ machine_structure)
        pipeline.add_documents_inline(get_context_files()[dirname], tag_surround)
        response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
    else:
        # Fallback to single-stage
        backend_status.write(f"  . . . :red[Failed to extract structure for {filename}.p. Falling back to single-stage generation.]")
        pipeline.add_user_msg(read_instructions()[MACHINE].format(machineName=filename))
        pipeline.add_documents_inline(get_context_files()[dirname], tag_surround)
        response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')

    log_token_count(pipeline, backend_status, f"Generate {filename}.p")
    backend_status.write(f"Running sanity check on response...")
    fixed_response = sanity_check(system_prompt, response, all_responses, machines_list, backend_status)
    log_filename, Pcode = extract_validate_and_log_Pcode(fixed_response, global_state.project_name_with_timestamp, dirname)
    return log_filename, Pcode


def generate_generic_file(system_prompt, design_doc_content,machines_list, filename, backend_status,
                                                                    dirname, all_responses):
    """Generate a generic P file."""
    pipeline = prompting_pipeline.PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_documents_inline(get_context_files()[dirname])
    pipeline.add_user_msg(read_instructions()[dirname].format(filename=filename),[global_state.P_basics_path])
    pipeline.add_user_msg(f"This is the Design Document for which I want you to generate the code for : /n {design_doc_content}")
    pipeline.add_user_msg(f"Other relevant files of this P Program that may contain declarations: {json.dumps(all_responses)}")
    response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
    log_token_count(pipeline, backend_status, f"Generate {filename}.p")
    backend_status.write(f"Running sanity check on response...")
    fixed_response = sanity_check(system_prompt, response, all_responses, machines_list, backend_status)
    log_filename, Pcode = extract_validate_and_log_Pcode(fixed_response, global_state.project_name_with_timestamp, dirname)
    return log_filename, Pcode


def extract_filenames(llm_response):
    """
    Extracts filenames from the LLM response and maps them to their respective folders.

    Parameters:
    llm_response (str): The response from the LLM containing filenames categorized by folders.

    Returns:
    filenames_map (dict): A dictionary where the keys are folder names and the values are lists of filenames without the '.p' extension.
    """
    folders = [PSRC, PSPEC, PTST]
    filenames_map = {folder: [] for folder in folders}

    lines = llm_response.split('\n')
    for line in lines:
        folder, files = line.split(': ')
        filenames_map[folder] = [file.strip().replace('.p', '') for file in files.split(',')]
    
    return filenames_map


def extract_validate_and_log_Pcode(llm_response, parent_dirname, dirname, logging_enabled=True):
    """
    Extracts the P code from the LLM response, validates it, and logs it.

    Parameters:
    llm_response (str): The response from the LLM containing P code enclosed within <*.p> tags.
    parent_dirname (str): The name of the parent directory where the log will be stored.
    dirname (str): The name of the subdirectory where the log will be stored.

    Returns:
    tuple: A tuple containing the filename (str) and the validated P code (str).
           If no matching content is found, returns None.
    """
    # for debugging original chatbot response
    log_utils.log_llmresponse(llm_response + "\n==================================================================\n")
    
    filename, Pcode = parse_llm_response_with_code(llm_response)
    if filename and Pcode:
        # Run manual syntax validation
        Pcode = compile_utils.run_manual_syntax_validation(Pcode)

        # Log validated P code
        if logging_enabled:
            log_utils.log_Pcode(Pcode, parent_dirname, dirname, filename)
        return filename, Pcode
    else:
        logger.error("No matching .p file content found in the input string.")
        return None, None

def parse_llm_response_with_code(llm_response):
    # Regular expression pattern to match content between <*.p> and </*.p> tags
    p_file_pattern = r'<(\w+\.p)>(.*?)</\1>'

    # Extract tag name and content
    match = re.search(p_file_pattern, llm_response, re.DOTALL)
    if match:
        filename = match.group(1)
        Pcode = match.group(2).strip()
        return filename, Pcode
    return None, None


def set_project_name_from_design_doc(userTextInput):
    """
    Extracts and sets the project name from the design document.

    Parameters:
    userTextInput (str): The content of the design document as a string.

    Sets:
    global_state.project_name (str): The project name extracted from the <title> tags, with spaces replaced by underscores.
    global_state.project_name_with_timestamp (str): The project name appended with a timestamp in the format 'YYYY_MM_DD_HH_MM_SS'.
    """
    # Regular expression pattern to find content inside <title> tags
    project_name_pattern = r'<title>(.*?)</title>'

    match = re.search(project_name_pattern, userTextInput, re.IGNORECASE)
    if match:
        global_state.project_name = match.group(1).strip().replace(" ", "_")
    
    timestamp = global_state.current_time.strftime('%Y_%m_%d_%H_%M_%S')
    global_state.project_name_with_timestamp = f"{global_state.project_name}_{timestamp}"


def sanity_check(system_prompt, response, all_responses, machines_list, backend_status):
    """
    Performs comprehensive sanity checks on a P code response using multiple focused tasks.

    Args:
        system_prompt: The system prompt for the LLM pipeline
        response: The LLM response containing P code to check
        all_responses: Dictionary containing all generated P files
        machines_list: List of machine names in the project
        backend_status: writing to streamlit interface

    Returns:
        Response string in the same format with fixed P code if changes were needed
    """

    # Get all .txt files from sanity_check directory
    sanity_check_dir = global_state.sanity_check_folder
    if not os.path.exists(sanity_check_dir):
        backend_status.write(f"Warning: {sanity_check_dir} directory not found")
        return response

    # Get all .txt files and sort them to ensure consistent order for all the files
    task_files = [f for f in os.listdir(sanity_check_dir) if f.endswith('.txt')]
    task_files.sort()
    if not task_files:
        backend_status.write(f"No .txt files found in {sanity_check_dir} directory")
        return response

    enums_types_events = all_responses.get("Enums_Types_Events.p", "")
    current_response = response

    # Process each task sequentially
    for i, task_file in enumerate(task_files, 1):
        backend_status.write(f"Running sanity check task {i}/{len(task_files)}: {task_file}")

        # Create a fresh pipeline for each task
        pipeline = prompting_pipeline.PromptingPipeline()
        pipeline.add_system_prompt(system_prompt)

        # Add the specific task instructions
        pipeline.add_user_msg(
            f"Please follow the instructions in the attached task file to check and fix P language compliance issues.",
            [global_state.P_basics_path, f"{sanity_check_dir}/{task_file}"]
        )

        # Add context and current response
        pipeline.add_user_msg(f"""
        CONTEXT:
        1. Core declarations (Enums_Types_Events.p):
        {enums_types_events}
        
        2. Machine names in project:
        {machines_list}
        
        3. Response to check and fix:
        {current_response}
        
        4. Other relevant files that may contain declarations:
        {json.dumps(all_responses, indent=2)}
        
        IMPORTANT: Do NOT change any logic or functionality. Only fix syntax and compliance issues
        as specified in the task file. Preserve all existing functionality while making syntax corrections.
        
        Please analyze this response for the specific task requirements and apply ONLY the fixes 
        relevant to this task. Return the response in the same format with <filename.p> tags.
        """)

        # Get the fixed response for this task
        fixed_response = pipeline.invoke_llm( global_state.model_id,candidates=1, heuristic='random')

        # Update current_response for the next task
        current_response = fixed_response

        # Optional: Add a small delay between tasks to avoid rate limiting
        time.sleep(0.5)

    return current_response


def get_latest_context(all_responses, machines_list):
    enums_types_events = {"Enums_Types_Events.p": all_responses["Enums_Types_Events.p"]}
    return {
        "declarations": enums_types_events,
        "all_files": list(all_responses.keys()),
        "machine_names": machines_list
    }


def compiler_analysis(system_prompt,all_responses,machines_list, num_of_iterations, backend_status, ctx_pruning=None):
    max_iterations = num_of_iterations
    recent_project_path = file_utils.get_recent_project_path()
    compilation_success, compilation_result = compile_utils.compile_Pcode(recent_project_path)
    if compilation_success:
        backend_status.write(f":white_check_mark: :green[Compilation succeeded in {max_iterations - num_of_iterations} iterations.]")
        logger.info(f"Compilation succeeded in {max_iterations - num_of_iterations} iterations.")
        return

    P_filenames_dict = regex_utils.get_all_P_files(compilation_result)
    while (not compilation_success and num_of_iterations > 0):
        file_name, line_number, column_number = compile_utils.parse_compile_error(compilation_result)
        current_iteration = max_iterations - num_of_iterations
        backend_status.write(f". . . :red[[Iteration #{current_iteration}] Compilation failed in {file_name} at line {line_number}:{column_number}. Fixing the error...]")
        #  Obtain the error message's information.
        custom_msg, file_path, file_contents = compile_utils.get_correction_instruction(P_filenames_dict, compilation_result)
        # Get latest context with most up-to-date file versions
        latest_context = get_latest_context(all_responses, machines_list)
        # Get the required specific filename from llm to fix the issue
        required_machines = get_required_filename_from_llm(system_prompt, latest_context, file_name, line_number,
                                                           column_number, custom_msg, file_contents, backend_status)
        additional_context = {}
        if required_machines:
            for machine_name in required_machines:
                additional_context[machine_name] = all_responses[machine_name]  # Access dictionary directly
        custom_msg += "\nReturn only the generated P code without any explanation attached. Return the P code enclosed in XML tags where the tag name is the filename."

        # Actual Pipeline call
        pipeline = prompting_pipeline.PromptingPipeline()
        pipeline.add_system_prompt(system_prompt)
        pipeline.add_documents_inline(get_context_files()["COMPILE"], tag_surround)

        # Send context and error information
        pipeline.add_user_msg(f"""
            Here is the current context for fixing the compilation error:

            1. Core declarations (types, events, enums):
            {json.dumps(latest_context["declarations"], indent=2)}

            2. Available machines in the program:
            {json.dumps(latest_context["machine_names"], indent=2)}

            3. Error details:
            - File to fix: {file_name}
            - Error location: Line {line_number}, Column {column_number}
            - Error message: {custom_msg}

            4. Current file contents:
            {file_contents}
            
            5. Additional Context : {additional_context}

            """)

        response = pipeline.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
        log_token_count(pipeline, backend_status, "Compiler Analysis")
        backend_status.write(f". . . . . . Compiling the fixed code...")
        log_filename, Pcode = extract_validate_and_log_Pcode(response, "", "", logging_enabled=False)
        if log_filename is not None and Pcode is not None:
            log_utils.log_Pcode_to_file(Pcode, file_path)
            all_responses[log_filename] = Pcode
        num_of_iterations -= 1

        # Log the diff
        log_utils.log_code_diff(file_contents, response, "After fixing the P code")
        compilation_success, compilation_result = compile_utils.compile_Pcode(recent_project_path)

    if compilation_success: 
        backend_status.write(f":green[Compilation succeeded in {max_iterations - num_of_iterations} iterations.]")
        logger.info(f"Compilation succeeded in {max_iterations - num_of_iterations} iterations.")
    global_state.compile_iterations += (max_iterations - num_of_iterations)
    global_state.compile_success = compilation_success


def get_required_filename_from_llm(system_prompt, latest_context, file_name, line_number, column_number, custom_msg, file_contents, backend_status):
    # Ask llm for the required specific file names - instead of sending all the files
    pipeline_for_req_files = prompting_pipeline.PromptingPipeline()
    pipeline_for_req_files.add_system_prompt(system_prompt)
    pipeline_for_req_files.add_user_msg(f"""
            Here is the current context for fixing the compilation error:

            1. Core declarations (types, events, enums):
            {json.dumps(latest_context["declarations"], indent=2)}
        

            2. Available machines in the program:
            {json.dumps(latest_context["machine_names"], indent=2)}
            3. Error details:
            - File to fix: {file_name}
            - Error location: Line {line_number}, Column {column_number}
            - Error message: {custom_msg}

            4. Current file contents:
            {file_contents}
            
            5. All Available Files :  {json.dumps(list(latest_context["all_files"]))}
            Before you proceed with fixing this compilation error, do you need the implementation details of any specific file from the All Available Files to understand the context better and to help fix the issue?. Note that the file must be existing in teh provided context
    
            Only respond with a JSON list of files that you need to see the implementation for without any additional explanation. If you don't need any additional machine context, return an empty list [].
            
            Example response format:
            ["MachineName1.p", "MachineName2.p", "Spec1.p", "TestDriver".p] or []
            """)

    required_machines_response = pipeline_for_req_files.invoke_llm(global_state.model_id, candidates=1, heuristic='random')
    logger.info("Required machines response: %s", repr(required_machines_response))

    if isinstance(required_machines_response, str):
        try:
            required_machines = json.loads(required_machines_response.strip())
        except json.JSONDecodeError as e:
            logger.error(f"Failed to parse JSON from required_machines_response: {repr(required_machines_response)}")
            backend_status.write(f"Failed to parse JSON from required_machines_response: {repr(required_machines_response)}")
            raise e
        except Exception as ex:
            logger.error("Exception occurred in compiler_analysis ")
            backend_status.write("Exception occurred in compiler_analysis ")
            raise ex
    else:
        logger.error(f"Expected string response, got {type(required_machines_response)} instead.")
        raise TypeError("Expected string response from LLM")

    return required_machines


def log_token_count(pipeline, backend_status, task = ""):
    input_tokens = pipeline.get_total_input_tokens()
    output_tokens = pipeline.get_total_output_tokens()
    logger.info(f"{task} :::: Input tokens: {input_tokens}, Output tokens: {output_tokens}")
    backend_status.write(f"{task} -  Total input tokens : {input_tokens}")
    backend_status.write(f"{task} - Total output tokens : {output_tokens}")


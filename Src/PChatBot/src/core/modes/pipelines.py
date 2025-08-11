from utils.module_utils import save_module_state, restore_module_state
from utils import file_utils, global_state, log_utils, string_utils, regex_utils, compile_utils, checker_utils
import re, os
from utils.project_structure_utils import setup_project_structure
from core.pipelining.prompting_pipeline import PromptingPipeline
from utils.constants import *
from utils.generate_p_code import extract_filenames, extract_validate_and_log_Pcode
from core.pipelining import examples as pipeline_examples
import json
import subprocess

def compiler_analysis(model_id, pipeline, all_responses, num_of_iterations, ctx_pruning=None):
    max_iterations = num_of_iterations
    recent_project_path = file_utils.get_recent_project_path()

    compilation_success, compilation_result = compile_utils.compile_Pcode(recent_project_path)
    if compilation_success:
        print(f":white_check_mark: :green[Compilation succeeded in {max_iterations - num_of_iterations} iterations.]")
        print(f"Compilation succeeded in {max_iterations - num_of_iterations} iterations.")
        return
    
    P_filenames_dict = regex_utils.get_all_P_files(compilation_result)
    while (not compilation_success and num_of_iterations > 0):
        file_name, line_number, column_number = compile_utils.parse_compile_error(compilation_result)
        print(f". . . :red[[Iteration #{(max_iterations - num_of_iterations)}] Compilation failed in {file_name} at line {line_number}:{column_number}. Fixing the error...]")
        # 1. Obtain the error message's information. 
        custom_msg, file_path, file_contents = compile_utils.get_correction_instruction(P_filenames_dict, compilation_result)
        
        # Continue the conversation to fix compiler errors
        # Apply context pruning before fixing errors
        # if ctx_pruning:
        #     original_messages = messages.copy()
        #     messages = ctx_pruning.prune_context(messages, file_name)
        #     ctx_pruning.log_context_metrics(original_messages, messages, MockStatus())
            
        pipeline.add_user_msg(custom_msg)
        response = pipeline.invoke_llm(model_id, candidates=1, heuristic='random')
        print(f". . . . . . Compiling the fixed code...")
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
        print("============================DEBUG NOW ===========")
        print(f"COMPILATION RESULT : {compilation_result}")
        print(f"COMPILATION STATUS : {compilation_success}")    
    if compilation_success: 
        print(f":green[Compilation succeeded in {max_iterations - num_of_iterations} iterations.]")
        # backend_status.write(f":green[Total compilation token usage - Input: {globals.model_metrics['inputTokens']} tokens, Output: {globals.model_metrics['outputTokens']} tokens]")
        print(f"Compilation succeeded in {max_iterations - num_of_iterations} iterations.")
    
    global_state.compile_iterations += (max_iterations - num_of_iterations)

    global_state.compile_success = compilation_success

def create_proj_files(project_root):
    
    # Create project structure with folders and pproj file
    # parent_abs_path = global_state.custom_dir_path or os.path.join(os.getcwd(), global_state.recent_dir_path)
    setup_project_structure(project_root, global_state.project_name)


def generate_machine_code(model, pipeline, instructions, filename, dirname):
    """Generate machine code using either two-stage or single-stage process."""
    # Stage 1: Generate structure
    pipeline.add_user_msg(instructions['MACHINE_STRUCTURE'].format(machineName=filename))
    pipeline.add_documents_inline(get_context_files()["MACHINE_STRUCTURE"], string_utils.tag_surround)

    stage1_response = pipeline.invoke_llm(model, candidates=1, heuristic='random')    
    structure_pattern = r'<structure>(.*?)</structure>'
    match = re.search(structure_pattern, stage1_response, re.DOTALL)
    
    if match:
        # Two-stage generation
        machine_structure = match.group(1).strip()
        print(f"  . . . Stage 2: Implementing function bodies for {filename}.p")
        
        pipeline.add_user_msg(instructions[MACHINE].format(machineName=filename)+ "\n\nHere is the starting structure:\n\n" + machine_structure)
        pipeline.add_documents_inline(get_context_files()["MACHINE"], string_utils.tag_surround)

        response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
        print(response)
    else:
        # Fallback to single-stage
        print(f"  . . . :red[Failed to extract structure for {filename}.p. Falling back to single-stage generation.]")
        pipeline.add_user_msg(instructions[MACHINE].format(machineName=filename))
        pipeline.add_documents_inline(get_context_files()["MACHINE"], string_utils.tag_surround)
        response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
        print(response)

    # token_usage = response["current_tokens"]
    # log_token_usage(token_usage, backend_status)
    
    return extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, dirname)

def generate_generic_file(model_id, pipeline, instructions, filename, dirname):

    pipeline.add_user_msg(instructions[dirname].format(filename=filename))
    pipeline.add_documents_inline(get_context_files()[dirname], string_utils.tag_surround)
    response = pipeline.invoke_llm(model_id, candidates=1, heuristic='random')
    
    return extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, dirname)

def old_chatbot_replicated(task, model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, out_dir=".tmp"):

    SAVED_GLOBAL_STATE = save_module_state(global_state)
    
    all_responses = {}
    
    dd_path = task
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
    
    destination_path = out_dir
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
    pipeline.add_documents_inline(get_context_files()["ENUMS_TYPES_EVENTS"], string_utils.tag_surround)
    response = pipeline.invoke_llm(model, candidates=1, heuristic='random')
    log_filename, Pcode = extract_validate_and_log_Pcode(response, global_state.project_name_with_timestamp, PSRC)
    
    file_abs_path = os.path.join(project_root, PSRC, log_filename)
    print(f":blue[. . . filepath: {file_abs_path}]")
    
    if log_filename is not None and Pcode is not None:
        all_responses[log_filename] = Pcode

    step_no = 2

    for dirname, filenames in global_state.filenames_map.items():
        if dirname != PSRC:
            print(f"Step {step_no}: Generating {dirname}")
            step_no += 1
            
        for filename in filenames:
            print(f"Generating file {filename}.p")
            if dirname == PSRC and filename in machines_list:
                log_filename, Pcode = generate_machine_code(model, pipeline, instructions, filename, dirname)
            else:
                log_filename, Pcode = generate_generic_file(model, pipeline, instructions, filename, dirname)

            log_file_full_path = os.path.join(project_root, dirname, log_filename)
            print(f":blue[. . . filepath: {log_file_full_path}]")
            if log_filename is not None and Pcode is not None:
                all_responses[log_filename] = Pcode

        print(f"Running the P compiler and analyzer on {dirname}...")
        num_iterations = 20 if dirname == PTST else 15
        compiler_analysis(model, pipeline, all_responses, num_iterations)
    
    final_compile_status = True if global_state.compile_success else False
    # return all_responses
    restore_module_state(global_state, SAVED_GLOBAL_STATE)
    return (pipeline, project_root, final_compile_status)


def reduce_trace_size(trace_log):
    lines = trace_log.splitlines()
    reduced = "\n".join(lines[-50:])
    return reduced

def generate_generic_analysis_prompt(p_code, trace_log, additional_user_guidance=""):
    with open(global_state.generate_files_to_fix_for_pchecker, "r") as file:
        template = file.read()
        llm_query = template.format(
            error_trace=reduce_trace_size(trace_log),
            p_code=p_code, 
            tool="PChecker",
            additional_error_info=additional_user_guidance
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

def generate_analysis_prompt(p_code, test_case, trace_log, error_category, additional_user_guidance=""):
    """Generate the prompt for analyzing P checker errors."""
    llm_query = generate_generic_analysis_prompt(p_code, trace_log, additional_user_guidance=additional_user_guidance)
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

def generate_fix_prompt(p_code, analysis, error_category, additional_user_guidance=""):
    with open(global_state.generate_fixed_file_for_pchecker, "r") as f:
        template = f.read()
        additional_analysis = f'<additional_user_guidance>\n{additional_user_guidance}\n</additional_user_guidance>' if additional_user_guidance else ''
        full_analysis = f"{analysis}\n{additional_analysis}"
        params = {"p_code": p_code, "fix_description": full_analysis}
        llm_query = template.format(**params)
    return llm_query

def generate_fix_patches_prompt(p_code, analysis, error_category, additional_user_guidance=""):
    with open(global_state.generate_fix_patches_for_file, "r") as f:
        template = f.read()
        additional_analysis = f'<additional_user_guidance>\n{additional_user_guidance}\n</additional_user_guidance>' if additional_user_guidance else ''
        full_analysis = f"{analysis}\n{additional_analysis}"
        params = {"p_code": p_code, "fix_description": full_analysis}
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
    p_code = string_utils.file_dict_to_prompt(current_project_state)
    prompt = generate_analysis_prompt(p_code, test_case, trace_log, error_category)
    analysis = request_and_save_analysis(pipeline, prompt, out_dir, error_category)
    
    # Get and save fix
    
    fix_prompt = generate_fix_prompt(p_code, analysis, error_category)
    fix_response = request_and_save_fix(pipeline, fix_prompt, out_dir, error_category)

    return fix_response

def request_analysis(pipeline, prompt):
    """Request error analysis from LLM and save the response."""
    pipeline.add_user_msg(prompt)
    pipeline.invoke_llm()
    analysis = pipeline.get_last_response()
    return analysis

def get_error_analysis(pipeline, current_project_state, test_case, trace_log, error_category, additional_user_guidance=""):
    p_code = string_utils.file_dict_to_prompt(current_project_state)
    prompt = generate_analysis_prompt(p_code, test_case, trace_log, error_category, additional_user_guidance=additional_user_guidance)
    return request_analysis(pipeline, prompt)


def ask_llm_which_files_it_needs(pipeline: PromptingPipeline, current_project_state, test_case, trace_log, error_category):
    file_list = "\n".join(list(current_project_state.keys()))
    template = file_utils.read_file(global_state.template_ask_llm_which_files_it_needs)
    query = template.format(file_list=file_list, error_trace=trace_log)
    pipeline.add_user_msg(query)
    pipeline.invoke_llm()
    return pipeline.get_last_response()

# def get_error_analysis_snappy(pipeline, current_project_state, test_case, trace_log, error_category):
#     # p_code = file_dict_to_prompt(current_project_state)
#     file_list = "\n".join(list(current_project_state.keys()))
#     prompt = generate_filenames_prompt(file_names, test_case, trace_log, error_category)
#     return request_analysis(pipeline, prompt)

def attempt_fix_error(pipeline, current_project_state, analysis, error_category, additional_user_guidance=""):
    p_code = string_utils.file_dict_to_prompt(current_project_state)
    fix_prompt = generate_fix_prompt(p_code, analysis, error_category, additional_user_guidance)
    fix_response = request_fix(pipeline, fix_prompt, error_category)
    return process_llm_response_with_tags(fix_response)

def attempt_fix_error_patches(pipeline, current_project_state, analysis, error_category, additional_user_guidance=""):
    p_code = string_utils.file_dict_to_prompt(current_project_state)
    fix_prompt = generate_fix_patches_prompt(p_code, analysis, error_category, additional_user_guidance)
    fix_response = request_fix(pipeline, fix_prompt, error_category)
    return string_utils.parse_patches_by_file(fix_response)
    
def request_fix(pipeline, prompt, error_category):
    pipeline.add_user_msg(f"{prompt}\n\nApply the recommended fixes")
    pipeline.invoke_llm()
    response = pipeline.get_last_response()
    return response

def apply_patch_correction(filename, contents, patches, err_msg):
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt("You respond in unified diff format for a given task. No talking.")
    pipeline.add_user_msg(
        f"""I got the following error 
{err_msg}
from whatthepatch library while trying to apply the patch for 
{filename} 
in the following full patch summary.
{patches}

Here are the contents for {filename}:
{string_utils.add_line_numbers(contents)}

Please fix the issue with the patch and give it back.
\n""")
    attempts = 0
    attempted_patches = []
    while True:
        if attempts < 5:
            pipeline.invoke_llm()
            new_patch = pipeline.get_last_response()
            if new_patch.startswith("`"):
                new_patch = "\n".join(new_patch.splitlines()[1:-1])
            attempted_patches.append(new_patch)

            print("---- new patch ----")
            print(new_patch)

            new_dict = string_utils.apply_patch_whatthepatch_per_file({filename:new_patch}, {filename:contents})
            (new_content, err_msg) = new_dict[filename]

            if not err_msg:
                print("FIXED PATCH!!!")
                return True, attempted_patches, new_content, pipeline.get_token_usage()
            else:
                pipeline.add_user_msg(f"Still failing with error {err_msg}. Try again.")
        else:
            break
        attempts += 1

    return False, attempted_patches, contents, pipeline.get_token_usage()

def create_base_pipeline_fewshot():
    # Get the absolute path to the project root directory
    current_file_path = os.path.abspath(__file__)
    # Go up 3 levels: file -> modes -> core -> src -> project_root
    project_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(current_file_path))))
    
    # Create absolute paths to the required files
    p_few_shot = os.path.join(project_root, 'resources', 'context_files', 'modular-fewshot', 'p-fewshot-formatted.txt')
    p_nuances = os.path.join(project_root, 'resources', 'context_files', 'p_nuances.txt')

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

# def dummy_test(**kwargs):
#     test_status_changed_callback = kwargs["cb_test_status_changed"]
#     print(kwargs["tests"])
#     test_status_changed_callback({k:True for k in kwargs["tests"]})


def test_fix_pchecker_errors(task, filter_tests=[], model=CLAUDE_3_7, temperature=1.0, n=1, heuristic='random', max_tokens=100000, top_p=0.999, **kwargs):
    """Test fixing P checker errors with multiple attempts."""
    user_quit = False
    test_name, project_root = task
    test_name_dir = f"{kwargs['out_dir']}/{test_name}"
    out_dir = f"{test_name_dir}/fix_attempts"
    progress_callback = kwargs["cb_progress"]
    test_status_changed_callback = kwargs["cb_test_status_changed"]
    cb_update_trace_logs = kwargs["cb_update_trace_logs"]
    cb_request_user_feedback = kwargs["cb_request_user_feedback"]

    # Setup
    pipeline = setup_fix_pipeline()
    prev_project_root = project_root
    new_project_root, current_project_state = setup_project_state(project_root, test_name_dir)
    prev_project_state = current_project_state
    
    
    orig_results, _, orig_trace_logs = checker_utils.try_pchecker(project_root, "/tmp")
    test_cases_to_fix = sorted([t for t in get_failing_test_names(orig_results) if t in filter_tests])
    subprocess.run(['cp', '-r', f'{project_root}/../std_streams', f"{test_name_dir}/original_std_streams"])
    all_tests_fixed = False
    latest_results = {}
    latest_trace_logs = {}
    total_tokens_input = 0
    total_tokens_output = 0

    for i, test_case in enumerate(test_cases_to_fix):
        max_attempts = 3
        grace_points = 1.0
        attempt = 0
        prev_error_category = identify_error_category(orig_trace_logs[test_case])
        error_category = prev_error_category

        
        if all_tests_fixed:
            break

        while attempt < max_attempts and not user_quit:
            # print(f"==== ({total_progress}%) TASK SUB-PROGRESS {compute_progress_percentage(i+(attempt/(max_attempts+0.1)), len(test_cases_to_fix))}% ====")
            print(f"FIXING TEST: {test_case}\nATTEMPT {attempt}")
            progress_callback(test_case, float(compute_progress_percentage(attempt+1, max_attempts))/100)
            attempt_dir = f"{out_dir}/{test_case}/attempt_{attempt}"
            
            current_results, trace_dicts, trace_logs = checker_utils.try_pchecker(new_project_root, f"{attempt_dir}/std_streams")
            cb_update_trace_logs(trace_logs)
            latest_results = current_results
            latest_trace_logs = trace_logs
            prev_project_root = new_project_root
            
            if not trace_dicts.keys():
                all_tests_fixed = True
                print("ALL TESTS FIXED!")
                test_status_changed_callback(latest_results)
                break  # No more errors to fix
            
            if test_case not in trace_dicts:
                print(f"TEST CASE FIXED: {test_case}")
                test_status_changed_callback(latest_results)
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

    return (pipeline, new_project_root, latest_results)


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

import os
from datetime import datetime, timezone
from utils.chat_history import ChatHistory

# Base paths
PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RESOURCES_DIR = os.path.join(PROJECT_ROOT, "resources")

# P Language Constants
REGION = 'us-west-2'


# model ids
# Mistral Large 2 (24.07)
model_id_mistral = "mistral.mistral-large-2407-v1:0"

# Claude 3 Sonnet
model_id_sonnet3 = "anthropic.claude-3-sonnet-20240229-v1:0"

# Claude 3.5 Sonnet
model_id_sonnet3_5 = "anthropic.claude-3-5-sonnet-20240620-v1:0"
model_id_sonnet3_5_v2 = "us.anthropic.claude-3-5-sonnet-20241022-v2:0"

# Claude 3.7 Sonnet
model_id_sonnet3_7 = "us.anthropic.claude-3-7-sonnet-20250219-v1:0"
model_id_sonnet4 = "us.anthropic.claude-sonnet-4-20250514-v1:0"
model_id_opus_4 = "us.anthropic.claude-opus-4-20250514-v1:0"

# Model-specific token limits
model_token_limits = {
    "us.anthropic.claude-opus-4-20250514-v1:0": 65536,
    "us.anthropic.claude-sonnet-4-20250514-v1:0": 65536,
    "us.anthropic.claude-3-7-sonnet-20250219-v1:0": 100000,
    "us.anthropic.claude-3-5-sonnet-20241022-v2:0": 100000,
    "us.anthropic.claude-3-5-sonnet-20240620-v1:0": 100000,
    "anthropic.claude-3-sonnet-20240229-v1:0": 100000,
    "mistral.mistral-large-2407-v1:0": 100000
}

# Default model and its token limit
model_id = model_id_sonnet3_7
maxTokens = model_token_limits[model_id]  # Initialize with default model's limit
temperature = 1.0
topP = 0.999

model_metrics = {
    "inputTokens": 0,
    "outputTokens": 0,
    "latencyMs": 0
}
compile_iterations = 0
compile_success = False
total_runtime = 0

project_name = "P_Chatbot"
project_name_with_timestamp = "P_Chatbot"
filenames_map = {}

current_time = datetime.now(timezone.utc)

# P Instruction Files
system_prompt_path = os.path.join(RESOURCES_DIR, "context_files", "about_p.txt")
P_syntax_guide_path = os.path.join(RESOURCES_DIR, "context_files", "P_syntax_guide.txt")
P_basics_path = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_basics.txt")
P_program_example_path = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_program_example.txt")
initial_instructions_path = os.path.join(RESOURCES_DIR, "instructions", "initial_instructions.txt")
generate_filenames_instruction_path = os.path.join(RESOURCES_DIR, "instructions", "generate_filenames.txt")
generate_enums_types_events_instruction_path = os.path.join(RESOURCES_DIR, "instructions", "generate_enums_types_events.txt")
generate_machine_instruction_path = os.path.join(RESOURCES_DIR, "instructions", "generate_machine.txt")
generate_modules_file_instruction_path = os.path.join(RESOURCES_DIR, "instructions", "generate_modules_file.txt")
generate_spec_files_instruction_path = os.path.join(RESOURCES_DIR, "instructions", "generate_spec_files.txt")
generate_test_files_instruction_path = os.path.join(RESOURCES_DIR, "instructions", "generate_test_files.txt")
generate_design_doc = os.path.join(RESOURCES_DIR, "instructions", "generate_design_doc.txt")
generate_code_description = os.path.join(RESOURCES_DIR, "instructions", "generate_code_description.txt")
generate_sop_spec = os.path.join(RESOURCES_DIR, "instructions", "generate_sop_spec.txt")
generate_files_to_fix_for_pchecker = os.path.join(RESOURCES_DIR, "instructions", "generate_files_to_fix_for_pchecker.txt")
generate_fixed_file_for_pchecker = os.path.join(RESOURCES_DIR, "instructions", "generate_fixed_file_for_pchecker.txt")
generate_machine_structure_path = os.path.join(RESOURCES_DIR, "instructions", "generate_machine_structure.txt")
generate_project_structure_path = os.path.join(RESOURCES_DIR, "instructions", "generate_project_structure.txt")
sanity_check_instructions_path = os.path.join(RESOURCES_DIR, "instructions", "p_code_sanity_check.txt")
sanity_check_folder = os.path.join(RESOURCES_DIR, "instructions", "sanity_checks")

template_path_hot_state_bug_analysis = os.path.join(RESOURCES_DIR, "instructions", "semantic-fix-sets", "hot-state", "analysis_prompt.txt")
template_ask_llm_which_files_it_needs = os.path.join(RESOURCES_DIR, "instructions", "streamlit-snappy", "1_ask_llm_which_files_it_needs.txt")
generate_fix_patches_for_file = os.path.join(RESOURCES_DIR, "instructions", "streamlit-snappy", "2_generate_fix_patches_for_file.txt")

# P Language Context Files
P_ENUMS_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_enums_guide.txt")
P_TYPES_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_types_guide.txt")
P_EVENTS_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_events_guide.txt")
P_MACHINES_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_machines_guide.txt")
P_STATEMENTS_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_statements_guide.txt")
P_MODULE_SYSTEM_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_module_system_guide.txt")
P_SPEC_MONITORS_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_spec_monitors_guide.txt")
P_TEST_CASES_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_test_cases_guide.txt")
P_PROGRAM_STRUCTURE_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_program_structure_guide.txt")
P_SYNTAX_SUMMARY = os.path.join(RESOURCES_DIR, "context_files", "modular-fewshot", "p-fewshot-formatted.txt")
P_COMPILER_GUIDE = os.path.join(RESOURCES_DIR, "context_files", "modular", "p_compiler_guide.txt")

# generated code
logs_dir_path = os.path.join(PROJECT_ROOT, "generated_code")
recent_dir_path = os.path.join(PROJECT_ROOT, "generated_code", "recent")
archive_base_dir_path = os.path.join(PROJECT_ROOT, "generated_code", "archive")
full_log_path = os.path.join(PROJECT_ROOT, "generated_code", "full_log.txt")
code_diff_log_path = os.path.join(PROJECT_ROOT, "generated_code", "code_diff_log.txt")
custom_dir_path = None

# generated docs
docs_recent_dir_path = os.path.join(SRC_DIR, "generated_docs")

# chat history files
chat_history_path = os.path.join(SRC_DIR, "chat_history")

# checker output folder name
pchecker_output_folder = "PCheckerOutput"
bugfinding_folder = "BugFinding"

pproj_template_path = os.path.join(RESOURCES_DIR, "assets", "pproj_template.txt")

# compiler error files
general_errors_list_path = os.path.join(RESOURCES_DIR, "compile_analysis", "generic_errors.json")
specific_errors_list_path = os.path.join(RESOURCES_DIR, "compile_analysis", "errors.json")

# user mode vs admin mode
current_mode = "admin"

chat_history = ChatHistory()

# Flag to track if we've already attempted a restart
has_restarted = False

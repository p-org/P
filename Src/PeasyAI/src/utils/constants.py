from utils import file_utils, global_state

CLAUDE_3_7 = "us.anthropic.claude-3-7-sonnet-20250219-v1:0"
# CLAUDE_3_7 = "us.anthropic.claude-sonnet-4-20250514-v1:0"

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
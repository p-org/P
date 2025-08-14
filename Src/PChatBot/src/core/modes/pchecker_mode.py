import streamlit as st
from dataclasses import dataclass, field
from typing import Dict, List, Optional, Tuple
from io import StringIO
import os
import shutil
import re
import time
from utils.chat_utils import generate_response, render_chat_messages
from utils import file_utils, checker_utils, compile_utils
from core.modes import pipelines
from datetime import datetime
from enum import Enum
import time
from core.pipelining.prompting_pipeline import PromptingPipeline
from utils import string_utils
from st_diff_viewer import diff_viewer
import streamlit_scrollable_textbox as stx
from pathlib import Path

class Stages(Enum):
    INITIAL = 0
    RUNNING_FILE_ANALYSIS = 1
    FILE_ANALYSIS_COMPLETE = 2
    RUNNING_ERROR_ANALYSIS = 3
    ERROR_ANALYSIS_COMPLETE = 4
    RUNNING_GET_FIX = 5
    GET_FIX_COMPLETE = 6
    RUNNING_APPLY_FIX = 7
    APPLY_FIX_COMPLETE = 8
    COMPILE_FIX_FAILED = 9
    COMPILE_FIX_PASSED = 10
    PCHECKER_FAILED = 11
    PCHECKER_PASSED = 12

@dataclass
class CheckerConfig:
    schedules: int = 100
    timeout_seconds: int = 20
    seed: str = "default-seed"

@dataclass
class InteractiveModeState:
    current_stage: Stages = Stages.INITIAL
    current_test_name: str = ""
    previous_test_name: str = ""
    current_project_state: Dict[str, str] = field(default_factory=dict)
    new_files_dict: Dict[str, str] = field(default_factory=dict) 
    patches: Dict[str, str] = ""
    new_project_state: Dict[str, str] = field(default_factory=dict) 
    current_error_analysis: str = ""
    current_error_category: str = ""
    previous_error_category: str = ""
    current_trace_log: str = ""
    tests_to_fix: List[str] = field(default_factory=list)
    selected_files: List[str] = field(default_factory=list)
    additional_user_guidance: str = ""
    current_pipeline: PromptingPipeline = pipelines.create_base_pipeline_fewshot()
    debug_str: str = ""
    recent_compile_output: str = ""
    patch_debug_info: Dict[str, tuple] = field(default_factory=dict) 
    patch_results_dict: Dict[str, tuple] = field(default_factory=dict) 
    remaining_faulty_patches_to_fix: Dict[str, tuple] = field(default_factory=dict) 
    tmp_project_dir: str = ""

@dataclass
class PCheckerState:
    config: CheckerConfig = field(default_factory=CheckerConfig)
    project_path: str = ""
    latest_project_path: str = ""
    results: Optional[Dict[str, bool]] = field(default=None)
    trace_dicts: Optional[Dict] = field(default=None)
    trace_logs: Optional[Dict] = field(default=None)
    interactive_mode_active = False
    current_interactive_mode_state: InteractiveModeState = field(default_factory=InteractiveModeState)
    fix_progress: Dict[str, float] = field(default_factory=dict)
    usage_stats: Dict[str, Dict[str, int]] = field(default_factory=dict)

if 'pchecker_state' not in st.session_state:
    st.session_state.pchecker_state = PCheckerState()

def update_test_statuses(status_dict):
    state = st.session_state.pchecker_state

    for test_name in status_dict:
        state.results[test_name] = status_dict[test_name]
    # st.rerun()

def update_trace_logs(trace_logs):
    state = st.session_state.pchecker_state

    for test_name in trace_logs:
        state.trace_logs[test_name] = trace_logs[test_name]

class PCheckerMode:
    def __init__(self):
        self.display_page()

    def handle_auto_fix(self, test_name: str, project_path: str, trace_dict: dict, trace_log: str, progress_bar=None, spinner_column=None):
        print("[handle_auto_fix]")
        """Handle auto fix for a single test"""
        state = st.session_state.pchecker_state


        def update_progress(name, new_progress_value):
            progress_bar.progress(new_progress_value)
            # state.fix_progress.get(name, new_progress_value)

        timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')

        with spinner_column:
            with st.spinner("Fixing...", show_time=False):
                _, new_project_path, _ = pipelines.test_fix_pchecker_errors(
                    ("ProjectName", state.latest_project_path), 
                    filter_tests=[test_name],
                    out_dir=f"/Users/ahmayun/Desktop/pchatbot/src/P-Chatbot/results/streamlit-ui/{timestamp}",
                    cb_progress=update_progress,
                    cb_test_status_changed=update_test_statuses,
                    cb_update_trace_logs=update_trace_logs
                )

                state.latest_project_path = new_project_path

    def fix_tests(self, project_path: str, failed_tests: List[str], trace_dicts: Dict, trace_logs: Dict, ui_elements=None):
        print("[fix_tests]")
        """Handle auto fix for all failed tests"""
        state = st.session_state.pchecker_state

        def update_progress(name, new_progress_value):
            ui_elements[name]["progress_bar"].progress(new_progress_value)
            # st.rerun()

        def handle_user_feedback_request(req):
            state.user_guidance_request = req


        timestamp = datetime.now().strftime('%Y-%m-%d-%H-%M-%S')
        
        with st.spinner("Attempting to auto fix all tests...", show_time=True):
            _, new_path, _ = pipelines.test_fix_pchecker_errors(
                ("ProjectName", state.latest_project_path), 
                filter_tests=failed_tests,
                out_dir=f"/Users/ahmayun/Desktop/pchatbot/src/P-Chatbot/results/streamlit-ui/{timestamp}",
                cb_progress=update_progress,
                cb_test_status_changed=update_test_statuses,
                cb_update_trace_logs=update_trace_logs,
                cb_request_user_feedback=handle_user_feedback_request,
            )

        print("Finished!")
        print(f"New project path {new_path}")
        state.latest_project_path = new_path

    def display_project_path_box(self, state):
        print("[display_project_path_box]")
        # Project path input
        state.project_path = st.text_input(
            "Enter the path to your P project:", 
            value=state.project_path,
            placeholder="/path/to/your/project"
        )

    def display_configuration_section(self, state):
        print("[display_configuration_section]")
        # Configuration settings
        with st.expander("Configuration Settings"):
            state.config.schedules = st.number_input(
                "Number of schedules", 
                min_value=1, 
                value=state.config.schedules
            )
            state.config.timeout_seconds = st.number_input(
                "Timeout (seconds)", 
                min_value=1, 
                value=state.config.timeout_seconds
            )
            state.config.seed = st.text_input(
                "Seed",
                value=state.config.seed
            )

    def _handle_project_submit(self, state):
        print("[_handle_project_submit]")
        if not state.project_path:
            st.error("Please enter a project path")
            return
            
        if not os.path.exists(state.project_path):
            st.error("Project path does not exist")
            return

        state.latest_project_path = state.project_path 
        state.usage_stats = {"cumulative": {"inputTokens": 0, "outputTokens":0}, "last_action": {"inputTokens": 0, "outputTokens":0}}
        
        with st.spinner("Getting Project State...."):
            state.results, state.trace_dicts, state.trace_logs = checker_utils.try_pchecker(
                state.latest_project_path,
                schedules=state.config.schedules,
                timeout=state.config.timeout_seconds
            )

    def display_submit_button(self, state):
        print("[display_submit_button]")
        st.button("Run Checker", on_click=lambda: self._handle_project_submit(state))

    def display_test_status(self, state, passed):
        print("[display_test_status]")
        symbol = "✅ [Passing]" if passed else "❌ [Failing]"
        st.write(f"{symbol}")


    def display_button_autofix_test(self, state, test_name, progress_bar, spinner_column):
        print("[display_button_autofix_test]")
        st.button("🔧 Auto Fix", key=f"fix_{test_name}", on_click=lambda: self.handle_auto_fix(
            test_name=test_name,
            project_path=state.latest_project_path,
            trace_dict=state.trace_dicts[test_name],
            trace_log=state.trace_logs[test_name],
            progress_bar=progress_bar,
            spinner_column = spinner_column     
        ))

    def display_checker_summary_row(self, state, test_name, passed):
        print("[display_checker_summary_row]")
        cols = st.columns([1, 3, 2, 3, 2])
        progress_bar = None
        spinner_column = cols[2]

        with cols[0]:
            self.display_test_status(state, passed)
        with cols[1]:
            st.write(f"{test_name}")
        if not passed:
            with cols[3]:
                progress = state.fix_progress.get(test_name, 0)
                progress_bar = st.progress(progress)
            with cols[4]:
                self.display_button_autofix_test(state, test_name, progress_bar, cols[2])

        return {"progress_bar": progress_bar, "spinner":spinner_column}

    def initialize_interactive_mode(self, state, failed_tests, ui_elements):
        print("[initialize_interactive_mode]")
        first_test_name = failed_tests[0]
        state.interactive_mode_active = True
        state.current_interactive_mode_state = InteractiveModeState()
        state.current_interactive_mode_state.tests_to_fix = failed_tests
        state.current_interactive_mode_state.current_test_name = first_test_name
        state.current_interactive_mode_state.current_stage = Stages.RUNNING_FILE_ANALYSIS
        state.current_interactive_mode_state.current_trace_log = state.trace_logs[first_test_name]
        state.current_interactive_mode_state.current_project_state = file_utils.capture_project_state(state.project_path)
        state.current_interactive_mode_state.current_error_category = pipelines.identify_error_category(state.trace_logs[first_test_name])


    def _handle_interactive_fix_all(self, state, failed_tests, ui_elements):
        print("[_handle_interactive_fix_all]")
        self.initialize_interactive_mode(state, failed_tests, ui_elements)
        # st.rerun()

    def display_button_interactive_fix_all(self, state, failed_tests, ui_elements):
        print("[display_button_interactive_fix_all]")
        st.button("🔧 Interactive Fix All", 
                  key="fix_all_interactive", 
                  on_click=lambda: self._handle_interactive_fix_all(state, failed_tests, ui_elements),
                  disabled=state.interactive_mode_active
                  )

    def _handle_autofix_all(self, state, failed_tests, ui_elements):
        print("[_handle_autofix_all]")
        self.fix_tests(
            project_path=state.latest_project_path,
            failed_tests=failed_tests,
            trace_dicts=state.trace_dicts,
            trace_logs=state.trace_logs,
            ui_elements=ui_elements
        )

    def display_button_autofix_all(self, state, failed_tests, ui_elements):
        print("[display_button_autofix_all]")
        st.button("🔧 Auto Fix All", key="fix_all", on_click=lambda: self._handle_autofix_all(state, failed_tests, ui_elements))

    def display_checker_summary(self, state):
        print("[display_checker_summary]")
        st.subheader("Project Checker Summary")

        ui_elements = {t:{} for t in state.results}
        failed_tests = sorted([t for t in state.results.keys() if not state.results[t]])
        
        for test_name, passed in sorted(state.results.items()):
            ui_elements[test_name] = {**self.display_checker_summary_row(state, test_name, passed)}
        
        if failed_tests:
            cols = st.columns([1, 2, 2, 1])
            with cols[1]:
                self.display_button_autofix_all(state, failed_tests, ui_elements)
            with cols[2]:
                self.display_button_interactive_fix_all(state, failed_tests, ui_elements)
    
    @st.dialog("Successfully Fixed!")
    def display_fix_success_dialog(self, state, im_state):
        print("[display_fix_success_dialog]")
        st.text(f"Test case {im_state.previous_test_name} was successfully fixed!")
        if st.button("Continue"):
            im_state.current_stage = Stages.RUNNING_FILE_ANALYSIS
            st.rerun()

    @st.dialog("Saved!")
    def display_save_success_dialog(self, state, im_state):
        st.text(f"Project was sucessfully saved!")
        if st.button("Close"):
            st.rerun()

    @st.dialog(f"Test case still Failing!")
    def display_fix_failed_dialog(self, state, im_state):
        print("[display_fix_failed_dialog]")
        st.text("Test case was not fixed!")
        st.markdown(f"**Previous Error Category:** {im_state.previous_error_category}")
        st.markdown(f"**New Error Category:** {im_state.current_error_category}")
        if st.button("Continue"):
            im_state.current_stage = Stages.RUNNING_FILE_ANALYSIS
            st.rerun()

    def display_file_selection_page(self, state, im_state):
        print("[display_file_selection_page]")
        st.subheader("Select Files for Analysis")
        
        # Categorize files
        project_files = []
        psrc_files = []
        pspec_files = []
        ptst_files = []
        
        for file_path in im_state.current_project_state.keys():
            if file_path.endswith('.pproj') or file_path.endswith('.ddoc'):
                project_files.append(file_path)
            elif file_path.startswith('PSrc/'):
                psrc_files.append(file_path)
            elif file_path.startswith('PSpec/'):
                pspec_files.append(file_path)
            elif file_path.startswith('PTst/'):
                ptst_files.append(file_path)
        
        # Display files in columns
        cols = st.columns(4)
        selected_files = []
        
        with cols[0]:
            st.markdown("##### Project")
            for file_path in project_files:
                is_checked = st.checkbox(
                    file_path,
                    value=file_path in im_state.selected_files,
                    key=f"file_checkbox_{file_path}"
                )
                if is_checked:
                    selected_files.append(file_path)
        
        with cols[1]:
            st.markdown("##### PSrc")
            for file_path in psrc_files:
                is_checked = st.checkbox(
                    file_path,
                    value=file_path in im_state.selected_files,
                    key=f"file_checkbox_{file_path}"
                )
                if is_checked:
                    selected_files.append(file_path)
        
        with cols[2]:
            st.markdown("##### PSpec")
            for file_path in pspec_files:
                is_checked = st.checkbox(
                    file_path,
                    value=file_path in im_state.selected_files,
                    key=f"file_checkbox_{file_path}"
                )
                if is_checked:
                    selected_files.append(file_path)
        
        with cols[3]:
            st.markdown("##### PTst")
            for file_path in ptst_files:
                is_checked = st.checkbox(
                    file_path,
                    value=file_path in im_state.selected_files,
                    key=f"file_checkbox_{file_path}"
                )
                if is_checked:
                    selected_files.append(file_path)
        
        im_state.selected_files = selected_files
        print(f"SELECTED FILES:")
        print('\n\t'.join(im_state.selected_files))
        
        if st.button("⌯⌲ Submit Selected Files"):
            im_state.current_stage = Stages.RUNNING_ERROR_ANALYSIS
            st.rerun()
    
    def display_error_analysis_page(self, state, im_state):
        st.subheader("LLM's Error Analysis")
        st.write(string_utils.tags_to_md(im_state.current_error_analysis))
        st.markdown("#### Do you agree with this analysis?")
        im_state.additional_user_guidance = st.text_area("Additional guidance:",placeholder="Optional")
        cols = st.columns([1,1,5])
        with cols[0]:
            if st.button("Agree ✅"): 
                im_state.current_stage = Stages.RUNNING_GET_FIX
                st.rerun()
        with cols[1]:
            if st.button("Disagree ❌"): 
                im_state.current_stage = Stages.RUNNING_ERROR_ANALYSIS
                st.rerun()


    def _handle_save_current_project(self, state, im_state, save_path):
        try:
            # Convert to Path objects for easier handling
            source_dir = Path(im_state.tmp_project_dir)
            target_dir = Path(save_path)
            
            # Check if source directory exists
            if not source_dir.exists():
                st.error(f"Source directory does not exist: {source_dir}")
                return
            
            # Create target directory if it doesn't exist
            target_dir.mkdir(parents=True, exist_ok=True)
            
            # Create a unique project folder name to avoid overwriting
            project_name = source_dir.name or "project"
            final_target = target_dir / project_name
            
            # If the target already exists, add a number suffix
            counter = 1
            while final_target.exists():
                final_target = target_dir / f"{project_name}_{counter}"
                counter += 1
            
            # Copy the entire directory tree
            shutil.copytree(source_dir, final_target)
            
            self.display_save_success_dialog(state, im_state)
            
        except PermissionError:
            st.error("Permission denied. Please check that you have write access to the selected folder.")
        except Exception as e:
            st.error(f"An error occurred while saving the project: {str(e)}")

    def display_current_goal(self, state, im_state):
        st.markdown(f"#### Current Goal")
        st.write(f"Fixing `{im_state.current_error_category}` for `{im_state.current_test_name}`")
        stx.scrollableTextbox(state.trace_logs[im_state.current_test_name], height=300)

    def display_save_recent_project(self, state, im_state):
        st.markdown("#### Save Most Recent Project")
        save_path = st.text_input("Enter the full path where you want to save the project", disabled=not im_state.tmp_project_dir, placeholder="e.g. /home/user/Desktop")
        st.button("Save", on_click=lambda : self._handle_save_current_project(state, im_state, save_path), disabled=not im_state.tmp_project_dir)
        if not im_state.tmp_project_dir:
            st.markdown("_There is no recent project yet_")

    def display_interactive_mode_control_center_header(self, state, im_state):
        print("[display_interactive_mode_control_center_header]")
        st.subheader("Interactive Mode Control Center")
        self.display_current_goal(state, im_state)
        self.display_save_recent_project(state, im_state)
        st.write("---")

    def update_usage_stats(self, state, usage_stats):
        state.usage_stats['cumulative']['inputTokens'] += usage_stats['cumulative']['inputTokens']
        state.usage_stats['cumulative']['outputTokens'] += usage_stats['cumulative']['outputTokens']
        state.usage_stats['last_action']['inputTokens'] = usage_stats['cumulative']['inputTokens']
        state.usage_stats['cumulative']['outputTokens'] = usage_stats['cumulative']['outputTokens']
        

    def llm_call_get_selected_files(self, state, im_state, current_test_name):
        im_state.current_pipeline = pipelines.create_base_pipeline_fewshot()
        file_list_str = pipelines.ask_llm_which_files_it_needs(
                im_state.current_pipeline,
                im_state.current_project_state, 
                current_test_name, 
                im_state.current_trace_log, 
                im_state.current_error_category
                )
        self.update_usage_stats(state, im_state.current_pipeline.get_token_usage())
        return file_list_str.split("\n")
        
    def run_analysis_files_needed(self, state, im_state):
        print("[run_analysis_files_needed]")

        current_test_name = im_state.current_test_name # always get the first test, will be removed as the checker goes on
        
        with st.spinner(f"Analyzing {current_test_name}..."):
            selected_files = self.llm_call_get_selected_files(state, im_state, current_test_name)
            im_state.selected_files = selected_files
        
        im_state.current_stage = Stages.FILE_ANALYSIS_COMPLETE
        st.rerun()

    def run_error_analysis(self, state, im_state):
        selected_files_dict = { f:im_state.current_project_state[f] for f in im_state.selected_files }
        
        im_state.current_pipeline = pipelines.create_base_pipeline_fewshot()
        with st.spinner("Analyzing error based on selected files ..."):
            im_state.current_error_analysis = pipelines.get_error_analysis(
                im_state.current_pipeline, 
                selected_files_dict, 
                im_state.current_test_name,
                im_state.current_trace_log,
                im_state.current_error_category,
                im_state.additional_user_guidance
                )
        im_state.current_stage = Stages.ERROR_ANALYSIS_COMPLETE
        im_state.additional_user_guidance = ""

        im_state.files_to_fix = [f.strip() for f in string_utils.extract_tag_contents(im_state.current_error_analysis, "files_to_fix").split(",")]
        
        self.update_usage_stats(state, im_state.current_pipeline.get_token_usage())
        # Can't use self.update_usage stats since this is a special case  the pipeline is being reused between this and the previous steps
        # state.usage_stats['cumulative']['inputTokens'] = im_state.current_pipeline.get_token_usage()["cumulative"]["inputTokens"]
        # state.usage_stats['cumulative']['outputTokens'] = im_state.current_pipeline.get_token_usage()["cumulative"]["outputTokens"]
        # state.usage_stats['last_action']['inputTokens'] = im_state.current_pipeline.get_token_usage()["sequential"][-1]["inputTokens"]
        # state.usage_stats['last_action']['outputTokens'] = im_state.current_pipeline.get_token_usage()["sequential"][-1]["outputTokens"]

        st.rerun()
        
    def run_get_fix(self, state, im_state):
        print("[run_get_fix]")
        selected_files_dict_numbered = { f:string_utils.add_line_numbers(im_state.current_project_state[f]) for f in im_state.selected_files }
        im_state.current_pipeline = pipelines.create_base_pipeline_fewshot()
        files_str = ",".join(im_state.selected_files)
        with st.spinner(f"Getting fix from LLM. Sent: {files_str}..."):
            patches = pipelines.attempt_fix_error_patches(
                im_state.current_pipeline,
                selected_files_dict_numbered,
                im_state.current_error_analysis,
                im_state.current_error_category,
                im_state.additional_user_guidance
            )

            im_state.patches = patches

        selected_files_dict = { f:im_state.current_project_state[f] for f in im_state.patches }
        im_state.patch_results_dict = string_utils.apply_patch_whatthepatch_per_file(im_state.patches, selected_files_dict)
        im_state.remaining_faulty_patches_to_fix = { k:(c,e) for k,(c,e) in im_state.patch_results_dict.items() if e }

        im_state.current_stage = Stages.GET_FIX_COMPLETE
        self.update_usage_stats(state, im_state.current_pipeline.get_token_usage())
        st.rerun()

    def display_debug_info_attempted_patches(self, state, im_state):
        for (filename, (fixed, attempted_patches)) in im_state.patch_debug_info.items():
            status = "FIXED" if fixed else "FAILED"
            with st.expander(f"[{status}] {filename}"):
                for i, ap in enumerate(attempted_patches):
                        with st.expander(f"Patch Attempt {i}"):
                            st.code(ap)

    def display_patch_debug_info(self, state, im_state):
        numbered_files_dict = { f:string_utils.add_line_numbers(im_state.current_project_state[f]) for f in im_state.selected_files }

        with st.expander("[DevTool] Debug info"):
            for filename in im_state.patches:
                with st.expander(f"Patch: {filename}"):
                    st.code(im_state.patches[filename])

            with st.expander("Numbered Files Dict"):
                for f in numbered_files_dict:
                    with st.expander(f):
                        st.code(numbered_files_dict[f])

            self.display_debug_info_attempted_patches(state, im_state)

    
    def run_faulty_patch_adjustment(self, state, im_state, filename, contents, err_msg):
        fixed, attempted_patches, patched_content, token_usage = pipelines.apply_patch_correction(
            filename, 
            contents, 
            im_state.patches[filename], 
            err_msg, 
            max_attempts=5
        )
        
        im_state.patch_results_dict[filename] = (patched_content, err_msg if not fixed else "")
        im_state.patch_debug_info[filename] = (fixed, attempted_patches)
        self.update_usage_stats(state, token_usage)
        del im_state.remaining_faulty_patches_to_fix[filename]

    def display_fix_diff(self, state, im_state):
        print("[display_fix_diff]")

        self.display_patch_debug_info(state, im_state)

        good_patch_files = [f for f in im_state.patch_results_dict if f not in im_state.remaining_faulty_patches_to_fix]
        faulty_patch_files = [f for f in im_state.remaining_faulty_patches_to_fix]
        print(f"faulty_patch_files = {faulty_patch_files}")

        ordered_files = good_patch_files + faulty_patch_files

        for filename in ordered_files:
            (contents, err_msg) = im_state.patch_results_dict[filename]
            st.markdown(f"###### 📄 {filename}")
            if filename in faulty_patch_files:
                with st.spinner(f"Fixing faulty patch for {filename}"):
                    self.run_faulty_patch_adjustment(state, im_state, filename, contents, err_msg)
                st.rerun()
            else:
                st.write(f"Orig {len(im_state.current_project_state[filename])} : {(len(contents))} New")
                with st.expander("Full Code"):
                    with st.expander(f"Original - {len(im_state.current_project_state[filename])} chars"):
                        st.code(im_state.current_project_state[filename])
                    with st.expander(f"New - {len(contents)} chars"):
                        st.code(contents)

                diff_viewer(
                    im_state.current_project_state[filename], 
                    contents, 
                    split_view=True,
                    disabled_word_diff=True
                    )
                if err_msg:
                    st.warning(f"{filename}: {err_msg}")
                
        cols = st.columns([1,1,5])
        with cols[0]:
            if st.button("Approve ✅"):
                im_state.new_files_dict = {k:c for (k, (c, _)) in im_state.patch_results_dict.items()}
                im_state.current_stage = Stages.RUNNING_APPLY_FIX
                st.rerun()

        with cols[1]:
            if st.button("Retry ❌"):
                im_state.current_stage = Stages.RUNNING_GET_FIX
                st.rerun()

    def run_apply_fix(self, state, im_state):
        print("[run_apply_fix]")
        with st.spinner("Applying fix..."):
            im_state.new_project_state = {**im_state.current_project_state, **im_state.new_files_dict}
            # checker_utils.try_pchecker_on_dict(new_project_state)

        im_state.current_stage = Stages.APPLY_FIX_COMPLETE
        st.rerun()

    def run_compile_fix(self, state, im_state):
        print("[run_compile_fix]")
        passed = False
        with st.spinner("Compiling fixed code.."):
            passed, tmp_dir, stdout = compile_utils.try_compile_project_state(im_state.new_project_state)
            im_state.tmp_project_dir = tmp_dir
            im_state.recent_compile_output = stdout
        
        im_state.current_stage = Stages.COMPILE_FIX_PASSED if passed else Stages.COMPILE_FIX_FAILED
        st.rerun()

    def display_compile_failed_page(self, state, im_state):
        print("[display_compile_failed_page]")
        st.subheader("Compilation Error")
        stx.scrollableTextbox(im_state.recent_compile_output, height = 300)

        with st.expander("[DevTools] Patches"):
            for filename, (patch, _) in im_state.patch_results_dict.items():
                with st.expander(f"{filename}"):
                    st.code(patch)

        if st.button("Regenerate Fix"):
            im_state.current_stage = Stages.RUNNING_GET_FIX
            st.rerun()
        
        if st.button("[DevTool] Assume fixed"):
            im_state.tmp_project_dir = state.latest_project_path
            im_state.current_stage = Stages.COMPILE_FIX_PASSED
            st.rerun()

    def move_to_next_test_case(self, state, im_state):
        failed_tests = sorted([t for t in state.results.keys() if not state.results[t]])
        next_test_name = failed_tests[0]

        im_state.current_project_state = {**im_state.new_project_state}
        im_state.current_test_name = next_test_name
        im_state.current_trace_log = state.trace_logs[next_test_name]
        im_state.new_files_dict = {}
        im_state.new_project_state = {}
        im_state.current_error_analysis = ""
        im_state.current_error_category = ""
        im_state.tests_to_fix = failed_tests
        im_state.selected_files = []
        im_state.additional_user_guidance = ""
        im_state.current_pipeline = pipelines.create_base_pipeline_fewshot()
        im_state.debug_str = ""
        im_state.recent_compile_output = ""

    def run_pchecker_on_fix(self, state, im_state):
        print("[run_pchecker_on_fix]")
        with st.spinner("Running PChecker on code..."):
            results, trace_dicts, trace_logs = checker_utils.try_pchecker(
                im_state.tmp_project_dir, 
                schedules=state.config.schedules, 
                timeout=state.config.timeout_seconds,
                seed=state.config.seed
                )
            
            current_test = im_state.current_test_name


            im_state.previous_error_category = im_state.current_error_category
            im_state.current_error_category = pipelines.identify_error_category(trace_logs[current_test])
            im_state.current_trace_log = trace_logs[current_test]


            state.results = results
            state.trace_dicts = trace_dicts
            state.trace_logs = trace_logs
            
            if results[current_test]:
                im_state.previous_test_name = current_test
                self.move_to_next_test_case(state, im_state)
                im_state.current_stage = Stages.PCHECKER_PASSED
            else:
                im_state.current_stage = Stages.PCHECKER_FAILED

        st.rerun()


    def display_interactve_mode_control_center(self, state):
        print("[display_interactve_mode_control_center]")
        
        im_state = state.current_interactive_mode_state
        self.display_interactive_mode_control_center_header(state, im_state)

        stage = im_state.current_stage
        print(f"STAGE = {stage}")

        if stage == Stages.RUNNING_FILE_ANALYSIS:
            self.run_analysis_files_needed(state, im_state)
        if stage == Stages.FILE_ANALYSIS_COMPLETE:
            self.display_file_selection_page(state, im_state)
        if stage == Stages.RUNNING_ERROR_ANALYSIS:
            self.run_error_analysis(state, im_state)
        if stage == Stages.ERROR_ANALYSIS_COMPLETE:
            self.display_error_analysis_page(state, im_state)
        if stage == Stages.RUNNING_GET_FIX:
            self.run_get_fix(state, im_state)
        if stage == Stages.GET_FIX_COMPLETE:
            self.display_fix_diff(state, im_state)
            if im_state.remaining_faulty_patches_to_fix:
                self.run_faulty_patch_adjustment(state, im_state)
        if stage == Stages.RUNNING_APPLY_FIX:
            self.run_apply_fix(state, im_state)
        if stage == Stages.APPLY_FIX_COMPLETE:
            self.run_compile_fix(state, im_state)
        if stage == Stages.COMPILE_FIX_FAILED:
            self.display_compile_failed_page(state, im_state)
        if stage == Stages.COMPILE_FIX_PASSED:
            self.run_pchecker_on_fix(state, im_state)
        if stage == Stages.PCHECKER_FAILED:
            self.display_fix_failed_dialog(state, im_state)
        if stage == Stages.PCHECKER_PASSED:
            self.display_fix_success_dialog(state, im_state)
        else:
            pass

    def append_to_log(self, state, line):
        state.log_lines.append(line)

    def display_token_metric(self, label: str, value: int):
        """Display a single token metric using Streamlit's metric component."""
        st.metric(
            label=label,
            value=f"{value:,}",
            delta=None
        )

    def display_cumulative_stats(self, stats: dict):
        """Display cumulative token usage statistics."""
        st.markdown("#### Cumulative Usage")
        metrics_col1, metrics_col2 = st.columns(2)
        with metrics_col1:
            self.display_token_metric("Input Tokens", stats['inputTokens'])
        with metrics_col2:
            self.display_token_metric("Output Tokens", stats['outputTokens'])

    def display_last_action_stats(self, stats: dict):
        """Display token usage statistics for the last action."""
        st.markdown("#### Last Action")
        metrics_col1, metrics_col2 = st.columns(2)
        with metrics_col1:
            self.display_token_metric("Input Tokens", stats['inputTokens'])
        with metrics_col2:
            self.display_token_metric("Output Tokens", stats['outputTokens'])

    def display_statistics_hud(self, state):
        if not state.usage_stats:
            return
        st.markdown("### 📊 Token Usage Statistics")
        col1, col2 = st.columns(2)
        with col1:
            self.display_cumulative_stats(state.usage_stats['cumulative'])
        with col2:
            self.display_last_action_stats(state.usage_stats['last_action'])

    def display_title_bar(self, state):
        st.set_page_config(page_title='Project Analysis Mode', layout='wide', page_icon =  "ui/assets/p_icon.ico")
        cols = st.columns([5,1,4])
        st.title('Project Analysis Mode')
        if state.results:
            if st.button("Reset"):
                st.session_state.pchecker_state = PCheckerState()
                st.rerun()


    def display_page(self):
        state = st.session_state.pchecker_state

        self.display_title_bar(state)

        if not state.results:
            self.display_project_path_box(state)

            self.display_configuration_section(state)
            
            self.display_submit_button(state)
        
        if state.results:
            self.display_checker_summary(state)
            st.markdown("---")
            if state.interactive_mode_active:
                self.display_statistics_hud(state)
                st.write("---")
                self.display_interactve_mode_control_center(state)

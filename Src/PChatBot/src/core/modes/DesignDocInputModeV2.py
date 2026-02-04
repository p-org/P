"""
Design Document Input Mode (V2) - Refactored to use Service Layer.

This module provides the Streamlit UI for generating P code from design documents,
using the new service layer and workflow engine.
"""

import streamlit as st
from io import StringIO
from typing import Optional
import os
from pathlib import Path

# Import the adapter
from ui.stlit.adapters import get_adapter


class DesignDocInputModeV2:
    """
    Streamlit mode for generating P code from design documents.
    
    Uses the workflow engine via StreamlitWorkflowAdapter for all
    generation operations.
    """
    
    def __init__(self):
        self.status_container = st.container()
        self.messages = st.container()
        self.bottom_path = st.container()
        
        # Initialize session state
        if "design_doc_v2_state" not in st.session_state:
            st.session_state.design_doc_v2_state = {
                "generated_files": {},
                "project_path": None,
                "success": None,
                "metrics": {}
            }
        
        self.display_page()
    
    def generate_from_design_doc(self):
        """Generate P code from the uploaded design document."""
        try:
            # Validate uploaded file
            uploaded_design_doc = st.session_state.get("design_doc")
            if not uploaded_design_doc:
                st.warning("Please upload a design document.", icon="⚠️")
                return
            
            # Read design doc content
            design_doc_content_io = StringIO(
                uploaded_design_doc.getvalue().decode("utf-8")
            )
            design_doc_content = design_doc_content_io.read()
            
            # Get destination path
            destination_path = st.session_state.get("destination_path", "").strip()
            if not destination_path:
                # Default to a generated_code directory
                project_root = Path(__file__).parent.parent.parent.parent
                destination_path = str(project_root / "generated_code" / "new_project")
            
            # Ensure directory exists
            os.makedirs(destination_path, exist_ok=True)
            
            # Show user message
            user_msg = f"📄 Uploaded Design Document: {uploaded_design_doc.name}"
            self.messages.chat_message("user").write(user_msg)
            
            # Get adapter and generate
            adapter = get_adapter()
            
            with self.status_container:
                result = adapter.generate_project(
                    design_doc=design_doc_content,
                    project_path=destination_path,
                    status_container=st
                )
            
            # Store results in session state
            state = st.session_state.design_doc_v2_state
            state["success"] = result.get("success", False)
            state["generated_files"] = result.get("files", {})
            state["project_path"] = result.get("project_path")
            state["metrics"] = result.get("metrics", {})
            
            # Display results
            if result.get("success"):
                self._display_success_results(result)
            else:
                self._display_error_results(result)
                
        except Exception as e:
            st.error(f"Error generating P code: {str(e)}", icon="⚠️")
            import traceback
            st.code(traceback.format_exc())
    
    def _display_success_results(self, result: dict):
        """Display successful generation results."""
        files = result.get("files", {})
        
        for filename, code in files.items():
            with self.messages.chat_message("assistant"):
                st.subheader(f"📄 {filename}")
                st.code(code, language="kotlin")
        
        # Show metrics
        metrics = result.get("metrics", {})
        with self.messages.expander("📊 Generation Metrics"):
            st.write(f"Steps Completed: {metrics.get('steps_completed', 'N/A')}")
            if result.get("completed_steps"):
                st.write("Completed Steps:")
                for step in result.get("completed_steps", []):
                    st.write(f"  ✅ {step}")
        
        # Show project path
        project_path = result.get("project_path")
        if project_path:
            self.bottom_path.success(f"📁 Project saved to: {project_path}")
    
    def _display_error_results(self, result: dict):
        """Display error results."""
        errors = result.get("errors", [])
        
        with self.messages.chat_message("assistant"):
            st.error("Generation completed with errors")
            
            if errors:
                st.subheader("Errors:")
                for error in errors:
                    st.error(error)
            
            # Still show any generated files
            files = result.get("files", {})
            if files:
                st.subheader("Partial Results:")
                for filename, code in files.items():
                    with st.expander(f"📄 {filename}"):
                        st.code(code, language="kotlin")
    
    def display_page(self):
        """Display the design document input page."""
        # Welcome message
        self.messages.chat_message("assistant").write(
            "👋 Hello! I'm a chatbot for generating P code.\n\n"
            "Upload a design document and I'll generate the corresponding P project."
        )
        
        # Input form
        with st.form("design_doc_form_v2", clear_on_submit=True):
            st.file_uploader(
                "📄 Upload a design document (.txt)",
                key="design_doc",
                type=["txt"],
                help="Upload a text file containing your P system design document."
            )
            
            st.text_input(
                "📁 Destination Path (optional)",
                key="destination_path",
                placeholder="/path/to/output/project",
                help="Leave empty to use default location."
            )
            
            col1, col2 = st.columns([1, 5])
            with col1:
                submitted = st.form_submit_button("🚀 Generate", type="primary")
            
            if submitted:
                self.generate_from_design_doc()
        
        # Show previous results if available
        state = st.session_state.design_doc_v2_state
        if state.get("success") is not None:
            st.divider()
            st.subheader("Previous Generation Results")
            
            if state["success"]:
                st.success("✅ Generation successful!")
                if state.get("project_path"):
                    st.info(f"📁 Project: {state['project_path']}")
            else:
                st.warning("⚠️ Generation had errors")
            
            # Button to clear
            if st.button("🗑️ Clear Results"):
                st.session_state.design_doc_v2_state = {
                    "generated_files": {},
                    "project_path": None,
                    "success": None,
                    "metrics": {}
                }
                st.rerun()


# For backward compatibility, alias as DesignDocInputMode
def DesignDocInputModeV2Wrapper():
    """Wrapper function for use in main app."""
    DesignDocInputModeV2()

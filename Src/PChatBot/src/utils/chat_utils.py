import streamlit as st
from utils import generate_p_code
from utils import global_state
import os

def generate_response(design_doc_content, status_container):
    """
    Call the LLM and obtain the LLM's response and metrics to display for the UI.

    Parameters:
    input_text (str): The input text to generate a response for.
    """
    try:
        with status_container.status("Generating P code... ", expanded = True) as status:
            final_response = generate_p_code.entry_point(design_doc_content, status)
            if not final_response:
                raise ValueError('Please try running the chatbot again.')
            st.session_state["response"] = final_response
            st.session_state["input_tokens"] = global_state.model_metrics["inputTokens"]
            st.session_state["output_tokens"] = global_state.model_metrics["outputTokens"]
            st.session_state["latency"] = global_state.model_metrics["latencyMs"]/1000
            st.session_state["total_runtime"] = global_state.total_runtime
            st.session_state["compile_iterations"] = global_state.compile_iterations
            if global_state.compile_success:
                status.update(label="P project generation complete! Compilation succeeded.", state="complete", expanded=False)
            else:
                status.update(label="P project generation complete! Compilation failed.", state="complete", expanded=False)     
            
    except Exception as e:
        st.error(" There was an error calling the LLM: " + str(e), icon="⚠️")

def render_chat_messages(messages):
    # Display chat messages from history on app rerun
    for message in global_state.chat_history.get_conversation():
        if message["download_path"] is not None and os.path.exists(message["download_path"]):
            with open(message["download_path"]) as f:
                messages.download_button('Download Design Doc', f, file_name=os.path.basename(message["download_path"]), key=message["download_path"])
        if message["chat_msg"]:
            messages.chat_message(message["role"]).write(message["chat_msg"])

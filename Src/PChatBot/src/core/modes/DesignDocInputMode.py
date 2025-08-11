import streamlit as st
from io import StringIO
from utils.chat_utils import generate_response
from utils import file_utils
from utils import global_state

class DesignDocInputMode:
    def __init__(self):
        self.status_container = st.container()
        self.messages = st.container()
        self.bottom_path = st.container() 

        self.display_page()


    def design_doc_func(self):
        _ = """
            This function corresponds to the design document P code generation button option. 
            Generates a response for when the user selects the design document chatbot option, then submits a design document.
            """
        try:
            # Validate and read the uploaded design doc from the user
            uploaded_design_doc = st.session_state.design_doc
            if not uploaded_design_doc:
                raise ValueError('Users must upload a design document.')
            # Validate id the destination path is a valid directory path
            destination_path = st.session_state.destination_path
            if destination_path and destination_path.strip() != "" and file_utils.check_directory(destination_path.strip()):
                global_state.custom_dir_path = destination_path.strip()
            design_doc_content_io = StringIO(uploaded_design_doc.getvalue().decode("utf-8"))
            design_doc_content = design_doc_content_io.read()
            user_inp = "User uploaded Design Document: " + uploaded_design_doc.name
            self.messages.chat_message("user").write(user_inp)
            global_state.chat_history.add_exchange("user", None, user_inp, None)

            # Call LLM
            generate_response(design_doc_content, self.status_container)

            # Set response in the chat
            if "response" in st.session_state:
                for file_name in st.session_state["response"]:
                    with self.messages.chat_message("assistant"):
                        st.subheader(file_name)
                        st.code(st.session_state["response"][file_name], language = "kotlin")
                        chat_msg = "### " + file_name + "\n```" + st.session_state["response"][file_name]
                        global_state.chat_history.add_exchange("assistant", None, chat_msg, None)
                with self.messages.expander(label="Metrics"):
                    st.write("Input Tokens:", st.session_state["input_tokens"], "tokens")
                    st.write("Output Tokens:", st.session_state["output_tokens"], "tokens")
                    st.write("Model Latency:", st.session_state["latency"], "seconds")
                    st.write("Compiler Iterations:", st.session_state["compile_iterations"], "iterations")
                    st.write("Total Latency:", st.session_state["total_runtime"], "seconds")
                msg_to_user = "Here is the local path to the project folder: " + file_utils.get_recent_project_path()
                global_state.custom_dir_path = None
                self.bottom_path.write(msg_to_user)
                global_state.chat_history.add_exchange("assistant", None, msg_to_user, None)
                global_state.chat_history.save_conversation()
                global_state.chat_history.clear_conversation()
                st.session_state['success'] = True

        except ValueError as e:
            st.warning("Design document generation didn't work. Please try running the LLM again!", icon="⚠️")
            st.session_state['success'] = False

    def display_page(self):
        self.messages.chat_message("assistant").write("Hello! I am a chatbot dedicated to generating P code! Please upload a design document. ")
        with st.form('design_doc_form', clear_on_submit=True):
            st.file_uploader("Upload a design document txt file!", key = 'design_doc', type=".txt", help = "For a template of the design document you would like to create, please download the template [here](https://drive.corp.amazon.com/documents/esthersu@/example_design_document.txt).")
            st.text_input("P Project Destination Path", key = "destination_path", help = "Destination folder path where the generated P project will be saved.")
            st.form_submit_button('Submit', on_click = self.design_doc_func)
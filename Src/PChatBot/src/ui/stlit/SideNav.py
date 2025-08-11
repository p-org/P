import streamlit as st
from utils import file_utils
from utils import global_state
import os
import json
from utils.ConversationHistory import ConversationHistory

class SideNav:
    def __init__(self):
        self.status_container = st.container()
        self.messages = st.container()
        self.bottom_path = st.container() 

        self.displaySideNav()

    def change_mode(self):
        """
        This function corresponds to the admin/user toggle. 
        It changes the tabs available to the user based on the admin/user toggle.
        """
        if st.session_state.get("toggle_mode", False):
            global_state.current_mode = "admin"
        else:
            global_state.current_mode = "user"

    def change_llm_model(self):
        """
        This function corresponds to the LLM dropdown menu. 
        It changes the LLM model being run based on the value selected by the user.
        """
        try:
            if st.session_state["llm_model"] == "Claude 3.7 Sonnet":
                global_state.model_id = global_state.model_id_sonnet3_7
            elif st.session_state["llm_model"] == "Claude 3.5 Sonnet v2":
                global_state.model_id = global_state.model_id_sonnet3_5_v2
            elif st.session_state["llm_model"] == "Claude 3.5 Sonnet":
                global_state.model_id = global_state.model_id_sonnet3_5
            elif st.session_state["llm_model"] == "Claude 3 Sonnet":
                global_state.model_id = global_state.model_id_sonnet3
            elif st.session_state["llm_model"] == "Mistral Large":
                global_state.model_id = global_state.model_id_mistral
        except Exception as e:
            st.warning("An error occured when switching the LLM through the LLM dropdwon menu.", icon="⚠️")


    def switch_main_menu(self):
        """
        This function corresponds to the home button in the UI.
        Navigates back to home page
        """
        st.session_state["display_history"] = False
        st.session_state['p_chatbot_state'] = None
        if "mode_state" in st.session_state:
            del st.session_state["mode_state"]
        global_state.chat_history.save_conversation()
        global_state.chat_history.clear_conversation()
        


    def change_temp(self):
        """
        This function corresponds to the temperature slider in the UI. 
        It changes the temperature value used by the chatbot.
        """
        global_state.temperature = st.session_state['temp']

    def change_top_p(self):
        """
        This function corresponds to the top P slider in the UI. 
        It changes the temperature value used by the chatbot.
        """
        global_state.topP = st.session_state['top_p']

    def displaySideNav(self):
        """
        Creates and displays components of side navigarion bar
        """
        with st.sidebar:
            # Home button to navigate back to home on click
            st.button("Home", on_click=self.switch_main_menu)

            # Creating and setting the user/admin toggle
            _ = """
                Creates two separate tabs if the user is in admin mode: History Tab and Configurations Tab
                Creates a single tab if the user is in user mode: History Tab
                """
            self.change_mode()
            what_mode = (
                "Admin Mode"
                if st.session_state.get("toggle_mode", False)
                else "User Mode"
            )
            mode = st.session_state.get("toggle_mode", False)
            st.toggle(what_mode, value=mode, key="toggle_mode", on_change = self.change_mode)

            if global_state.current_mode == "admin":
                history, configurations = st.tabs(["P Chatbot History", "Configurations"])
                configurations.title("Configurations")
                configurations.selectbox("Which Large Language model would you like to use today?", ("Claude 3.7 Sonnet", "Claude 3.5 Sonnet v2", "Claude 3.5 Sonnet", "Claude 3 Sonnet", "Mistral Large"), on_change=self.change_llm_model, key="llm_model")
                configurations.slider("Temperature", 0.0, 1.0, global_state.temperature, key = "temp", on_change=self.change_temp)
                configurations.write("Change the temperature of the P chatbot to increase or decrease the creativity of the chatbot's responses!")
                configurations.slider("Top P", 0.0, 1.0, global_state.topP, key = "top_p", on_change=self.change_top_p)
                configurations.write("Sample from the smallest possible set of tokens whose cumulative probability exceeds the threshold p!")
            else:
                history = st.tabs(["P Chatbot History"])[0]

            # Display the History Tab's Contents
            history.title("P Chatbot History")
            ConversationHistory(self.messages, history)
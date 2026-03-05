import streamlit as st
from datetime import datetime, timezone
from utils import file_utils
import os
import json

class ChatHistory:
    def __init__(self):
        if 'conversation' not in st.session_state:
            st.session_state.conversation = []
        self.start_timestamp = self.get_current_timestamp()
        self.end_timestamp = None
        self.chat_history_dir = "chat_history"
        self.title = None

    def get_current_timestamp(self):
        return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

    def add_exchange(self, role, query, response,download_path):
        # add to the conversation
        exchange = {'role': role, 'query_sent_to_llm': query, 'chat_msg': response,'download_path':download_path}
        if "conversation" not in st.session_state:
            st.session_state["conversation"] = []
            self.title = None
            self.start_timestamp = None
        if self.title == None and role == "user":
            self.title = response
            self.start_timestamp = self.get_current_timestamp()
        st.session_state.conversation.append(exchange)


    def get_conversation(self):
        # Retrieve the entire conversation
        if 'conversation' not in st.session_state:
            st.session_state.conversation = []
            self.title = None
            self.start_timestamp = None
        return st.session_state.conversation

    def clear_conversation(self):
        # Clear the conversation for a fresh start
        st.session_state.conversation = []
        self.title = None
        self.start_timestamp = None

    def save_conversation(self):
        if 'conversation' not in st.session_state:
            return
        # Saves the conversation to a file
        if (len(st.session_state.conversation) > 1):
            self.end_timestamp = self.get_current_timestamp()
            filename = "chat_history_" + self.start_timestamp + "_" + self.end_timestamp + ".json"
            file_path = os.path.join(self.chat_history_dir, filename)
            content = {"title": self.title, "start_timestamp": self.start_timestamp, "end_timestamp": self.end_timestamp, "messages": st.session_state.conversation}
            file_utils.write_file(file_path, json.dumps(content))



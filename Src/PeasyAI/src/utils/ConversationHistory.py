import streamlit as st
from utils import file_utils
from utils import global_state
from pathlib import Path
from datetime import datetime
import os
import json

class ConversationHistory:
    def __init__(self, messages, history):
        self.history = history
        self.messages = messages
        self.sorted_history_files = self.get_sorted_history_files()
        self.display_history()

    def display_history(self):
        options = self.get_pagination_options()
        if len(options) > 0:
            page_range = self.history.selectbox('Page:', options)
            self.display_history_for_range(page_range)
        else:
            self.display_history_for_range(None)


    def get_pagination_options(self):
        # Generate page ranges for dropdown
        max_per_page = 5
        options = []
        start_index = 0
        end_index = 0
        no_of_history_files = len(self.sorted_history_files)
        while start_index < no_of_history_files:
            end_index = min(start_index + max_per_page, no_of_history_files)
            options.append(str(start_index) + "-" + str(end_index - 1))
            start_index += max_per_page
        return options

    def display_history_for_range(self, range):
        if range:
            start_index, end_index = list(map(int, range.split("-")))
            sorted_info = self.sorted_history_files[start_index : end_index + 1]
            for info in sorted_info:
                filepath = os.path.join(global_state.chat_history_path, info[0]) 
                
                self.history.button(info[1][:100] + "...", on_click=self.display_interaction, args = [filepath], key=info[2])
        else:
            self.history.write("No history to show")

    def display_interaction(self, filepath):
        try:
            st.session_state["display_history"] = True
            messages = json.loads(file_utils.read_file(filepath))["messages"]
            for message in messages:
                self.messages.chat_message(message["role"]).write(message["chat_msg"])

        except Exception as e:
            st.warning("An error occured when displaying chat history ", icon="⚠️")
            print(e)
    
    def get_sorted_history_files(self):
        if file_utils.check_directory(global_state.chat_history_path):
            all_history_files = file_utils.list_top_level_contents(global_state.chat_history_path)
            
            # Extract end time from filename
            end_timestamps = []
            titles = []
            for history_file in all_history_files:
                title, _, end_timestamp = self.get_history_info(history_file)
                titles.append(title)
                end_timestamps.append(end_timestamp)

            combined = list(zip(all_history_files, titles, end_timestamps))

            # Sort the combined list based on timestamps (list2)
            sorted_file_info = sorted(combined, key=lambda x: datetime.strptime(x[2], "%Y-%m-%dT%H:%M:%SZ"), reverse=True)

        
            # Print sorted filenames
            return sorted_file_info
        else:
            file_utils.create_directory(global_state.chat_history_path)
            return []
        
    def get_history_info(self, history_file):
        file_path = os.path.join(global_state.chat_history_path, history_file)
        content = json.loads(file_utils.read_file(file_path))
        return content["title"], content["start_timestamp"], content["end_timestamp"]
        
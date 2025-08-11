import streamlit as st
# from utils.global_state import chat_history
from core.modes.DesignDocInputMode import DesignDocInputMode
# from chatbot_modes.InteractiveMode import InteractiveMode
from ui.stlit.SideNav import SideNav
from core.modes.pchecker_mode import PCheckerMode

# Set up the browser page configurations, including tab title and tab icon
st.set_page_config(page_title='P Chatbot', layout='wide', page_icon =  "ui/assets/p_icon.ico")

# Title and containers for different sections of the webpage
st.title('P Chatbot')
back_button, clear_button = st.columns([1.5, 11]) # Container for the topmost "Back to Start Page" button and "Clear" button
coding_language = "kotlin"

def is_home_page():
    return 'p_chatbot_state' not in st.session_state or st.session_state['p_chatbot_state'] is None

def is_chat_history_page():
    return "display_history" in st.session_state and st.session_state["display_history"]

def change_chatbot_state(new_state):
    """
    Change the chatbot state where each state contains different ways to interact with the chatbot.

    Parameters:
    new_state (int): The new state to set for the chatbot.
    """
    st.session_state['p_chatbot_state'] = new_state

def back():
    st.session_state['p_chatbot_state'] = None
    if "mode_state" in st.session_state:
        del st.session_state["mode_state"]
    # chat_history.save_conversation()
    # chat_history.clear_conversation()
    

def display_page():
    st.empty()
    SideNav()

    # If showing past interactions from chat history, don't display any chatbot mode 
    if is_chat_history_page():
        return
    
    if is_home_page():
        display_home_page()
    else:
        # back_button.button('Back to Start Page', on_click=back)
        if st.session_state['p_chatbot_state'] == "DesignDocInputMode":
            DesignDocInputMode()
        elif st.session_state['p_chatbot_state'] == "PCheckerMode":
            PCheckerMode()
        elif st.session_state['p_chatbot_state'] == "InteractiveMode":
            InteractiveMode()
        else:
            display_home_page()                
    

def display_home_page():
    st.session_state['p_chatbot_state'] = None
    st.session_state["display_history"] = False

    st.write("Hi! I'm an AI Assistant trained on writing P Code. What would you like me to do today?")
    st.button("Generate P Code from a design doc", on_click=change_chatbot_state, args = ["DesignDocInputMode"])
    st.button("Run PChecker on P project", on_click=change_chatbot_state, args=["PCheckerMode"])
    st.button("P Code Interactive", on_click=change_chatbot_state, args = ["InteractiveMode"])


def main():
    display_page()

main()

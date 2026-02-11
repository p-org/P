def save_module_state(module):
    state = {}
    for attr in dir(module):
        if not attr.startswith("__"):  # Skip built-in attributes
            state[attr] = getattr(module, attr)
    return state

def restore_module_state(module, state):
    for var_name, value in state.items():
        setattr(module, var_name, value)
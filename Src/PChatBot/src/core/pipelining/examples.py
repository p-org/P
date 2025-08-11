from core.pipelining.prompting_pipeline import PromptingPipeline

def create_base_pipeline_fewshot():
    p_few_shot = 'resources/context_files/modular-fewshot/p-fewshot-formatted.txt'
    p_nuances = 'resources/context_files/p_nuances.txt'

    system_prompt = \
        "You are a chatbot dedicated to help with the P language. " + \
        "P provides a high-level state machine based programming language to formally model and specify distributed systems. " + \
        "P supports specifying and checking both safety as well as liveness specifications (global invariants)." + \
        "Avoid discussing and writing anything other than P." + \
        "Only write code, no commentary."
    
    initial_msg = 'Here are some information relevant to P.'
    
    pipeline = PromptingPipeline()
    pipeline.add_system_prompt(system_prompt)
    pipeline.add_user_msg(initial_msg, documents = [p_few_shot,p_nuances])
    return pipeline
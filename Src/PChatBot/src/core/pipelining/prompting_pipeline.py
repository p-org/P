import boto3
import json
from botocore.exceptions import ClientError
from botocore.config import Config
from pathlib import Path
from utils import file_utils, global_state
import os
import utils.constants as constants


DEFAULT_TOOL = {
        "toolSpec": {
            "name": "NOP",
            "description": "This tool does nothing.",
            "inputSchema": {
                "json": {
                    "type": "object",
                    "properties": {
                        "x": {
                            "type": "number",
                            "description": "The number to pass to the function."
                        }
                    },
                    "required": ["x"]
                }
            }
        }
    }

class PromptingPipeline:

    def __init__(self):
        # Initialize with empty context list in the required format
        self.conversation = []
        self.system_prompt = None
        self.usage_stats = {'cumulative': {}, 'sequential': []}
        self.removed_messages = []

    def add_system_prompt(self, prompt):
        # Add system prompt as assistant role
        self.system_prompt = [{"text": prompt}]

    def add_text(self, text):
        # Add text as user role
        self.conversation.append({
            "role": "user",
            "content": [
                {
                    "text": text
                }
            ]
        })

    def remove_last_messages(self, n=1):
        self.removed_messages.append(self.conversation[-n:])
        self.conversation = self.conversation[:-n]

    def add_document(self, doc_path):
        # Add document as user role with document content
        path = Path(doc_path)
        
        self.conversation.append({
            "role": "user",
            "content": [
                self._create_document_entry(path)
            ]
        })

    def _create_document_entry(self, path):
        content = file_utils.read_file(path.resolve())
        return {
            "document": {
                "name": path.stem,
                "format": path.suffix.lstrip('.'),
                "source": {
                    "bytes": content.encode("utf-8")
                }
            }
        }

    def add_documents_inline(self, doc_list, pre="", post="", preprocessing_function=lambda _, c:c):
        # Process all documents into a single string
        combined_text = file_utils.combine_files(doc_list, pre, post, preprocessing_function)
        # Add the combined text as a single text item
        self.conversation.append({
            "role": "user",
            "content": [
                {
                    "text": combined_text.strip()
                }
            ]
        })

    def _create_bedrock_client(self):
        """Create and return a configured Bedrock client"""
        config = Config(read_timeout=1000)
        return boto3.client(service_name='bedrock-runtime', region_name='us-west-2', config=config)

    def _get_single_response(self, bedrock_client, model, inference_config, tool_config={"tools": [DEFAULT_TOOL]}):

        try:
            response = bedrock_client.converse(
                modelId = model,
                messages = self.conversation,
                system = self.system_prompt,
                inferenceConfig = inference_config,
                # toolConfig=tool_config
            )
        except Exception as e:
            class BytesEncoder(json.JSONEncoder):
                def default(self, obj):
                    if isinstance(obj, bytes):
                        return f"{obj}"

                    return super().default(obj)
            print("======= VALIDATION EXCEPTION WHILE CALLING CONVERSE ======== ")
            # print("------ CONVERSATION ------")
            # print(self.conversation)
            os.makedirs(".err", exist_ok=True)
            conv_dict = {"conv": self.conversation}
            with open(".err/conversation.json", 'w') as f:
                json.dump(conv_dict, f, indent=4, cls=BytesEncoder)
            print("------ USAGE STATS ------")
            print(self.usage_stats)
            with open(".err/usage_stats.json", 'w') as f:
                json.dump(self.usage_stats, f, indent=4, cls=BytesEncoder)
            print("="*30)
            raise e

        return response

    def _select_response(self, responses, heuristic='random'):
        """Select a response using the specified heuristic"""
        if heuristic == 'random':
            import random
            return random.choice(responses)
        # TODO: Add other heuristics here as needed
        
        return responses[0]  # Default to first response

    def invoke_llm(
                    self, 
                    model=constants.CLAUDE_3_7, 
                    candidates=1, 
                    heuristic='random', 
                    inference_config=None,
                    tool_config = {"tools": [DEFAULT_TOOL]}):
        if inference_config is None:
            inference_config = {
                "maxTokens": global_state.maxTokens,
                "temperature": global_state.temperature,
                "topP": global_state.topP
            }

        bedrock_client = self._create_bedrock_client()
        
        responses = []
        for _ in range(candidates):
            response_dict = self._get_single_response(bedrock_client, model, inference_config, tool_config=tool_config)
            # print(response_dict)
            if response_dict['stopReason'] == "tool_use":
                print(response_dict)
                input("tool called")

            self.update_usage_stats(response_dict)
            response = response_dict["output"]["message"]["content"][0]["text"]
            responses.append(response)
        
        selected_response = self._select_response(responses, heuristic)
        
        self.conversation.append(self._create_assistant_msg(selected_response))
        return selected_response
    
    def _create_assistant_msg(self, msg):
        return {
            "role": "assistant",
            "content": [
                {
                    "text": msg
                }
            ]
        }

    def get_last_response(self):
        # Find the last assistant message in context
        for message in reversed(self.conversation):
            if message["role"] == "assistant":
                return message["content"][0]["text"]
        return None

    def prune_context(self, pruner_function):
        self.conversation = pruner_function(self.conversation)

    def get_context(self):
        return self.conversation

    def add_user_msg(self, msg, documents=[]):
        document_entries = list(map(lambda p: self._create_document_entry(Path(p)), documents))
        self.conversation.append({
            "role": "user",
            "content": [
                {
                    "text": msg
                },
                *document_entries
            ]
        })

    def add_assistant_msg(self, msg):
        self.conversation.append(self._create_assistant_msg(msg))

    def get_conversation(self):
        return self.conversation
    
    def get_system_prompt(self):
        return self.system_prompt
    
    def update_cumulative_stats(self, usage_dict):
        for k, v in usage_dict.items():
            cumu_dict = self.usage_stats['cumulative']
            if k not in cumu_dict:
                cumu_dict[k] = 0
            self.usage_stats['cumulative'][k] += v

    def update_usage_stats(self, response):
        usage_dict = response['usage']
        self.update_cumulative_stats(usage_dict)
        self.usage_stats['sequential'].append(usage_dict)
    
    def get_token_usage(self):
        return self.usage_stats
    
    def get_total_input_tokens(self):
        return self.usage_stats['cumulative']['inputTokens']

    def get_total_output_tokens(self):
        return self.usage_stats['cumulative']['outputTokens']


# =================== SAMPLE CONVERSATION FROM ORIGINAL IMPLEMENTATION ============================
# """
# [
#     {
#         "role": "user",
#         "content": [
#             {
#                 "text": "Read the attached P language basics guide for reference. You can refer to this document to understand P syntax and answer accordingly. Additional specific syntax guides will be provided as needed for each task."
#             },
#             {
#                 "document": {
#                     "name": "P_basics_guide",
#                     "format": "txt",
#                     "source": {
#                         "bytes": "48657265206973207468652062617369632050206c616e67756167652067756964653a0a3c67756964653e0a4120502070726f6772616d20636f6e7369737473206f66206120636f6c6c656374696f6e206f6620666f6c6c6f77696e6720746f702d6c6576656c206465636c61726174696f6e733a0a312e20456e756d730a322e205573657220446566696e65642054797065730a332e204576656e74730a342e205374617465204d616368696e65730a352e2053706563696669636174696f6e204d6f6e69746f72730a362e20476c6f62616c2046756e6374696f6e730a372e204d6f64756c652053797374656d0a0a4865726520697320746865206c697374206f6620616c6c20776f726473207265736572766564206279207468652050206c616e67756167652e20546865736520776f72647320686176652061207370656369616c206d65616e696e6720616e6420707572706f73652c20616e6420746865792063616e6e6f742062652075736564206173206964656e7469666965727320666f72207661726961626c65732c20656e756d732c2074797065732c206576656e74732c206d616368696e65732c2066756e6374696f6e20706172616d65746572732c206574632e3a0a3c72657365727665645f6b6579776f7264733e0a7661722c20747970652c20656e756d2c206576656e742c206f6e2c20646f2c20676f746f2c20646174612c2073656e642c20616e6e6f756e63652c20726563656976652c20636173652c2072616973652c206d616368696e652c2073746174652c20686f742c20636f6c642c2073746172742c20737065632c206d6f64756c652c20746573742c206d61696e2c2066756e2c206f627365727665732c20656e7472792c20657869742c20776974682c20756e696f6e2c20666f72656163682c20656c73652c207768696c652c2072657475726e2c20627265616b2c20636f6e74696e75652c2069676e6f72652c2064656665722c206173736572742c207072696e742c206e65772c2073697a656f662c206b6579732c2076616c7565732c2063686f6f73652c20666f726d61742c2069662c2068616c742c20746869732c2061732c20746f2c20696e2c2064656661756c742c20496e746572666163652c20747275652c2066616c73652c20696e742c20626f6f6c2c20666c6f61742c20737472696e672c207365712c206d61702c207365742c20616e790a3c2f72657365727665645f6b6579776f7264733e0a3c2f67756964653e0a"
#                     }
#                 }
#             }
#         ]
#     },
#     {
#         "role": "assistant",
#         "content": [
#             {
#                 "text": "I understand. I will refer to the P language guides to provide accurate information about P syntax when answering questions."
#             }
#         ]
#     },
#     {
#         "role": "user",
#         "content": [
#             {
#                 "text": "hi\n\nReference P Language Guide:\nHere is the basic P language guide:\n<guide>\nA P program consists of a collection of following top-level declarations:\n1. Enums\n2. User Defined Types\n3. Events\n4. State Machines\n5. Specification Monitors\n6. Global Functions\n7. Module System\n\nHere is the list of all words reserved by the P language. These words have a special meaning and purpose, and they cannot be used as identifiers for variables, enums, types, events, machines, function parameters, etc.:\n<reserved_keywords>\nvar, type, enum, event, on, do, goto, data, send, announce, receive, case, raise, machine, state, hot, cold, start, spec, module, test, main, fun, observes, entry, exit, with, union, foreach, else, while, return, break, continue, ignore, defer, assert, print, new, sizeof, keys, values, choose, format, if, halt, this, as, to, in, default, Interface, true, false, int, bool, float, string, seq, map, set, any\n</reserved_keywords>\n</guide>\n"
#             }
#         ]
#     }
# ]
# """

# =================== SAMPLE BEDROCK RESPONSE DICT ============================
# {
#     'ResponseMetadata': {
#         'RequestId': 'c4a81505-b723-4f67-a816-3abbd1f52739', 
#         'HTTPStatusCode': 200, 
#         'HTTPHeaders': {
#             'date': 'Tue, 24 Jun 2025 21:05:43 GMT', 'content-type': 'application/json', 'content-length': '1975', 'connection': 'keep-alive', 'x-amzn-requestid': 'c4a81505-b723-4f67-a816-3abbd1f52739'
#             }, 
#         'RetryAttempts': 0
#         }, 
#     'output': {
#         'message': {
#             'role': 'assistant', 
#             'content': [{'text': '```\ntype tToggleReq = (source: Switch, switchId: int);\ntype tStatusResp = (switchId: int, isOn: bool);\nevent eToggleReq : tToggleReq;\nevent eStatusResp: tStatusResp;\n\nmachine Switch {\n\n    var light: Light;\n    var switchId: int;\n    var expectedState: bool;\n    var statusStr: string;\n\n    start state Init {\n        entry (lightParam: Light, idParam: int, initStateParam: bool) {\n            light = lightParam;\n            switchId = idParam;\n            expectedState = initStateParam;\n            statusStr = "";\n            goto ReadyToToggle;\n        }\n    }\n\n    state ReadyToToggle {\n        entry {\n            send light, eToggleReq, (source = this, switchId = switchId);\n            expectedState = !expectedState;\n        }\n\n        on eStatusResp do (resp: tStatusResp) {\n\n            if(resp.isOn) {\n                statusStr = "ON";\n            } else {\n                statusStr = "OFF";\n            }\n            \n            print format("Switch {0}: Light is now {1}", switchId, statusStr);\n\n        }\n    }\n}\n\nmachine Light {\n    var isOn: bool;\n    \n    start state Off {\n        entry {\n            isOn = false;\n        }\n\n        on eToggleReq do (req: tToggleReq) {\n            isOn = true;\n            send req.source, eStatusResp, (switchId = req.switchId, isOn = isOn);\n            goto On;\n        }\n    }\n\n    state On {\n        entry {\n            isOn = true;\n        }\n\n        on eToggleReq do (req: tToggleReq) {\n            isOn = false;\n            send req.source, eStatusResp, (switchId = req.switchId, isOn = isOn);\n            goto Off;\n        }\n    }\n}\n```'}]
#             }
#         }, 
#     'stopReason': 'end_turn', 
#     'usage': {
#         'inputTokens': 1448, 
#         'outputTokens': 487, 
#         'totalTokens': 1935, 
#         'cacheReadInputTokens': 0, 
#         'cacheWriteInputTokens': 0
#         }, 
#     'metrics': {'latencyMs': 6610}
# }
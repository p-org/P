import sys

request_qps = int(sys.argv[1])
response_qps = int(sys.argv[2])
max_time = int(sys.argv[3])
n_server_fails = int(sys.argv[4])
k_client_amplification = int(sys.argv[5])
n_retries = int(sys.argv[6])

test_content = None
with open("PTst/TestDriver.p", "r") as f:
    test_content = f.readlines()

assert test_content is not None

test_content[16] = test_content[16][:26] + str(request_qps) + ";\n"
test_content[17] = test_content[17][:27] + str(response_qps) + ";\n"
test_content[18] = test_content[18][:22] + str(max_time) + ";\n"
test_content[19] = test_content[19][:27] + str(n_server_fails) + ";\n"
test_content[20] = test_content[20][:35] + str(k_client_amplification) + ";\n"
test_content[21] = test_content[21][:23] + str(n_retries) + ";\n"

with open("PTst/TestDriver.p", "w") as f:
    f.writelines(test_content)

import sys

shards = int(sys.argv[1])
rho = float(sys.argv[2])
back_pressure = bool(sys.argv[3])

test_content = None
with open("PTst/Test.p", "r") as f:
    test_content = f.readlines()

assert test_content is not None

test_content[18] = test_content[18][:17] + str(shards) + ";\n"
test_content[19] = test_content[19][:22] + str(float(shards)) + ";\n"
test_content[20] = test_content[20][:14] + str(rho) + ";\n"
test_content[21] = test_content[21][:23] + str(back_pressure).lower() + ";\n"

with open("PTst/Test.p", "w") as f:
    f.writelines(test_content)

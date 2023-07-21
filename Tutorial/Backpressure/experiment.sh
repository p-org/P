

for shards in {1..2}; do
	for rho in 0.02; do
		for back_pressure in True False; do
			echo "Modifying test case with shards = ${shards}, rho = {$rho}, back_pressure = {$back_pressure}"
			/usr/bin/python3 modify_test.py $shards $rho $back_pressure
			echo "Compiling the P program..."
			pl compile
			echo "Simulating the P program..."
			pl check --sch-statistical -i 1
			echo "Analyzing the trace..."
			/usr/bin/python3 analyzer.py
		done
	done
done
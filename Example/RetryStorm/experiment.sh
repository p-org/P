echo "avg_t,p99_t,n_requests,n_responses,success_percentage,request_qps,response_qps,max_time,n_server_fails,k_client_amplification"
for n_server_fails in 0 1 2 3 4 5; do
	for k_client_amplification in 1; do
		for response_qps in 10; do
			for max_time in 100; do
				for request_qps in {1..10}; do
					/usr/bin/python3 modify_test.py $request_qps $response_qps $max_time $n_server_fails $k_client_amplification
					dotnet /Users/beyazity/Documents/P/Bld/Drops/Release/Binaries/net6.0/p.dll compile 1> out.log 2> err.log
					dotnet /Users/beyazity/Documents/P/Bld/Drops/Release/Binaries/net6.0/p.dll check --sch-statistical -i 1 -ms 1000000000 1> out.log 2> err.log
					/usr/bin/python3 analyzer.py $request_qps $response_qps $max_time $n_server_fails $k_client_amplification 2> err.log
					rm err.log
					rm out.log
				done
			done
		done
	done
done

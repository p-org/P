# P-Chatbot-Bedrock

This chatbot generates P code.

## Documentation

Generated documentation for the latest released version can be accessed here:
https://devcentral.amazon.com/ac/brazil/package-master/package/go/documentation?name=P-Chatbot-Bedrock&interface=1.0&versionSet=live

## Development

See instructions in DEVELOPMENT.md

### Instructions to Run the Chatbot
1. Ensure your roles and users are set up correctly by running:
```vim ~/.aws/credentials```. 
You need to to have a Bedrock secret access key and access key id from a user in AWS IAM with Bedrock permissions. Here is an example:
```
[bedrock]
aws_access_key_id=<access key>
aws_secret_access_key=<secret access key>
```
2. Navigate to the `~/P-Chatbot` directory.
3. Run the following lines of code.
```
python3 -m venv path/to/venv
source path/to/venv/bin/activate
source .env
```
4. Run the following command to install all the required dependencies.
```
python3 -m pip install -r requirements.txt
```
5. Run the following line to run the chatbot.
```
cd P-Chatbot
streamlit run src/app.py
``` 


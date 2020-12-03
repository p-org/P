import re, sys

tag_pattern = re.compile("^p\-([0-9]+)\.([0-9]+)\.([0-9]+)$")
version_pattern = re.compile("^([0-9]+)\.([0-9]+)\.([0-9]+)$")
if not tag_pattern.match(sys.argv[1]):
	raise ValueError("Tag %s does not match pattern" % sys.argv[1])
if not version_pattern.match(sys.argv[2]):
	raise ValueError("Version %s does not match pattern" % sys.argv[2])
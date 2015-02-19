#pragma once
#include<string>
#include<iostream>
#include <windows.h>
#include <thread>
#include<fstream>

using namespace std;

#define _CRT_SECURE_NO_WARNINGS

boolean _ROBOCOPY(string source, string dest);
void _CONCAT(char* dest, char* string1, char* string2);
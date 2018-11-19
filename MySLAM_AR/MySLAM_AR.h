#pragma once
#include <jni.h>
#include <unistd.h>
#include <android/log.h>
class MySLAM_AR
{
public:
	const char * getPlatformABI();
	MySLAM_AR();
	~MySLAM_AR();
};


#include <jni.h>
#include <unistd.h>
#include <android/log.h>
#include <errno.h>

#include <string.h>
#include <sys/resource.h>

#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "MySLAM_Native", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "MySLAM_Native", __VA_ARGS__))

typedef void(*ProgressChangedCallback)(int progress);
extern ProgressChangedCallback gProgressChangedCallback;

extern "C" JNIEXPORT void JNICALL MySLAM_Native_RegisterProgressChangedCallback(ProgressChangedCallback callback);
extern "C" JNIEXPORT void JNICALL MySLAM_Native_UnRegisterProgressChangedCallback();

extern "C" JNIEXPORT void JNICALL MySLAM_Native_InitSystem();

extern "C" JNIEXPORT int JNICALL MySLAM_Native_GetPose(long long mataddress, long long timestamp, float* out);

extern "C" JNIEXPORT void JNICALL MySLAM_Native_ReleaseMap();


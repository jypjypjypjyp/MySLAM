#include <jni.h>
#include <unistd.h>
#include <android/log.h>
#include <errno.h>

#include <string.h>
#include <sys/resource.h>

#define LOGI(...) ((void)__android_log_print(ANDROID_LOG_INFO, "MySLAM_Native", __VA_ARGS__))
#define LOGW(...) ((void)__android_log_print(ANDROID_LOG_WARN, "MySLAM_Native", __VA_ARGS__))
#define LOGE(...) ((void)__android_log_print(ANDROID_LOG_ERROR, "MySLAM_Native", __VA_ARGS__))

typedef void(*ProgressChangedCallback)(int progress);
extern ProgressChangedCallback gProgressChangedCallback;

extern "C" JNIEXPORT void JNICALL RegisterProgressChangedCallback(ProgressChangedCallback callback);
extern "C" JNIEXPORT void JNICALL UnRegisterProgressChangedCallback();

extern "C" JNIEXPORT void JNICALL InitSystem();

extern "C" JNIEXPORT int JNICALL UpdateTracking(long long mataddress, long long timestamp);
extern "C" JNIEXPORT void JNICALL EstimatePose(float* data, int n, long long timestamp, float* pose);

extern "C" JNIEXPORT void JNICALL ReleaseMap();


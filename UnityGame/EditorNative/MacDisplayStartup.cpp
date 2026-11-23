#include <CoreFoundation/CoreFoundation.h>
#include <stdio.h>

// Unity 6000.6 macOS uses this C++ entry point in its generated executable.
extern int PlayerMain(int argc, const char** argv);

static int ReadSize(CFStringRef domain, CFStringRef key)
{
    CFPropertyListRef value = CFPreferencesCopyValue(
        key, domain, kCFPreferencesCurrentUser, kCFPreferencesAnyHost);
    int size = 0;
    if (value != NULL) {
        if (CFGetTypeID(value) == CFNumberGetTypeID())
            CFNumberGetValue((CFNumberRef)value, kCFNumberIntType, &size);
        CFRelease(value);
    }
    return size;
}

static Boolean MigrateResolution(CFStringRef domain)
{
    CFStringRef widthKey = CFSTR("Screenmanager Resolution Width Default");
    CFStringRef heightKey = CFSTR("Screenmanager Resolution Height Default");
    const int width = ADAM_DEFAULT_WIDTH;
    const int height = ADAM_DEFAULT_HEIGHT;

    if (ReadSize(domain, CFSTR("Screenmanager Resolution Width")) <= 0 ||
        ReadSize(domain, CFSTR("Screenmanager Resolution Height")) <= 0)
        return true;
    if (ReadSize(domain, widthKey) == width && ReadSize(domain, heightKey) == height)
        return true;

    // Match the new build defaults so Unity uses the live resolution and mode keys.
    CFNumberRef newWidth = CFNumberCreate(NULL, kCFNumberIntType, &width);
    CFNumberRef newHeight = CFNumberCreate(NULL, kCFNumberIntType, &height);
    const void *keys[] = { widthKey, heightKey };
    const void *values[] = { newWidth, newHeight };
    CFDictionaryRef defaults = CFDictionaryCreate(NULL, keys, values, 2,
        &kCFTypeDictionaryKeyCallBacks, &kCFTypeDictionaryValueCallBacks);
    CFPreferencesSetMultiple(defaults, NULL, domain, kCFPreferencesCurrentUser, kCFPreferencesAnyHost);
    CFRelease(defaults);
    CFRelease(newHeight);
    CFRelease(newWidth);
    return CFPreferencesSynchronize(domain, kCFPreferencesCurrentUser, kCFPreferencesAnyHost);
}

int main(int argc, char *argv[])
{
    CFBundleRef bundle = CFBundleGetMainBundle();
    CFStringRef domain = bundle == NULL ? NULL : CFBundleGetIdentifier(bundle);
    if (domain == NULL) {
        fputs("Adam could not identify its preference domain. Reinstall or verify the game files.\n", stderr);
        return 1;
    }
    if (!MigrateResolution(domain)) {
        fputs("Adam could not update the saved display defaults. Check access to your preferences and retry.\n", stderr);
        return 1;
    }
    return PlayerMain(argc, const_cast<const char**>(argv));
}

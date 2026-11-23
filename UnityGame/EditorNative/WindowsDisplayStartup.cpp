#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <cstdint>
#include <string>
#include "AdamDisplayDefaults.h"

extern "C" {
__declspec(dllexport) DWORD NvOptimusEnablement = 1;
__declspec(dllexport) DWORD AmdPowerXpressRequestHighPerformance = 1;
}

static std::wstring PlayerPrefsName(const wchar_t* key)
{
    std::uint32_t hash = 5381;
    for (const wchar_t* character = key; *character != L'\0'; ++character)
        hash = (hash * 33) ^ *character;
    return std::wstring(key) + L"_h" + std::to_wstring(hash);
}

static LSTATUS ReadSize(HKEY prefs, const std::wstring& name, DWORD& value)
{
    DWORD size = sizeof(value);
    return RegGetValueW(prefs, nullptr, name.c_str(), RRF_RT_REG_DWORD, nullptr, &value, &size);
}

static LSTATUS UpdateDefault(HKEY prefs, const wchar_t* key, DWORD size)
{
    const auto name = PlayerPrefsName(key);
    DWORD previous = 0;
    if (ReadSize(prefs, name, previous) == ERROR_SUCCESS && previous == size)
        return ERROR_SUCCESS;
    return RegSetValueExW(prefs, name.c_str(), 0, REG_DWORD,
        reinterpret_cast<const BYTE*>(&size), sizeof(size));
}

static LSTATUS MigrateResolution()
{
    HKEY prefs;
    const auto open = RegOpenKeyExW(HKEY_CURRENT_USER, ADAM_PREFS_PATH, 0, KEY_QUERY_VALUE, &prefs);
    if (open == ERROR_FILE_NOT_FOUND || open == ERROR_PATH_NOT_FOUND)
        return ERROR_SUCCESS;
    if (open != ERROR_SUCCESS)
        return open;

    DWORD width = 0, height = 0, oldWidth = 0, oldHeight = 0;
    const bool saved = ReadSize(prefs, PlayerPrefsName(L"Screenmanager Resolution Width"), width) == ERROR_SUCCESS
        && ReadSize(prefs, PlayerPrefsName(L"Screenmanager Resolution Height"), height) == ERROR_SUCCESS
        && width > 0 && height > 0;
    const bool current = saved
        && ReadSize(prefs, PlayerPrefsName(L"Screenmanager Resolution Width Default"), oldWidth) == ERROR_SUCCESS
        && ReadSize(prefs, PlayerPrefsName(L"Screenmanager Resolution Height Default"), oldHeight) == ERROR_SUCCESS
        && oldWidth == ADAM_DEFAULT_WIDTH && oldHeight == ADAM_DEFAULT_HEIGHT;
    RegCloseKey(prefs);
    if (!saved || current)
        return ERROR_SUCCESS;

    const auto reopen = RegOpenKeyExW(HKEY_CURRENT_USER, ADAM_PREFS_PATH, 0, KEY_QUERY_VALUE | KEY_SET_VALUE, &prefs);
    if (reopen != ERROR_SUCCESS)
        return reopen;
    // A partial write is retryable. Do not start Unity until both defaults match.
    const auto updated = UpdateDefault(prefs, L"Screenmanager Resolution Width Default", ADAM_DEFAULT_WIDTH);
    const auto result = updated == ERROR_SUCCESS
        ? UpdateDefault(prefs, L"Screenmanager Resolution Height Default", ADAM_DEFAULT_HEIGHT)
        : updated;
    RegCloseKey(prefs);
    return result;
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE previous, PWSTR arguments, int show)
{
    if (MigrateResolution() != ERROR_SUCCESS) {
        MessageBoxW(nullptr, L"Check access to your saved display settings and retry.",
            L"Adam could not update the saved display defaults", MB_OK | MB_ICONERROR);
        return 1;
    }

    // Use the executable directory and System32, never the caller's working directory.
    const auto player = LoadLibraryExW(L"UnityPlayer.dll", nullptr,
        LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
    using PlayerEntry = int (__cdecl *)(HINSTANCE, HINSTANCE, PWSTR, int);
    const auto entry = player == nullptr ? nullptr
        : reinterpret_cast<PlayerEntry>(GetProcAddress(player, "UnityMain"));
    if (entry == nullptr) {
        MessageBoxW(nullptr, L"Reinstall or verify the game files.",
            L"Adam could not start its Unity Player", MB_OK | MB_ICONERROR);
        return 1;
    }
    return entry(instance, previous, arguments, show);
}

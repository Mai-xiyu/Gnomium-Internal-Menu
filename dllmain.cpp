#include <windows.h>
#include <iostream>

DWORD WINAPI MainThread(HMODULE hModule)
{
    // 1. 分配控制台用于调试输出
    AllocConsole();
    FILE* f;
    freopen_s(&f, "CONOUT$", "w", stdout);

    std::cout << "[+] DLL 注入成功！" << std::endl;
    std::cout << "[+] 正在寻找 Mono 运行时..." << std::endl;

    // 2. 获取 Mono 模块
    HMODULE hMono = GetModuleHandle(L"mono-2.0-bdwgc.dll");
    if (!hMono) {
        std::cout << "[-] 未找到 mono-2.0-bdwgc.dll" << std::endl;
    } else {
        std::cout << "[+] 成功定位 mono-2.0-bdwgc.dll (" << hMono << ")" << std::endl;
        
        // 此处可通过 GetProcAddress 动态获取 mono_thread_attach, mono_get_root_domain 等函数
        // 从而调用 C# 方法或加载自定义 C# Payload
    }

    // 3. 阻塞，等待退出指令 (按 END 键退出)
    while (!GetAsyncKeyState(VK_END))
    {
        Sleep(100);
    }

    // 4. 清理并退出
    fclose(f);
    FreeConsole();
    FreeLibraryAndExitThread(hModule, 0);
    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        CloseHandle(CreateThread(nullptr, 0, (LPTHREAD_START_ROUTINE)MainThread, hModule, 0, nullptr));
        break;
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

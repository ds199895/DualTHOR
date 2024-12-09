from pathlib import Path
import subprocess
import time

# 构造相对路径：从当前脚本文件目录出发，定位到 Unity 可执行文件
UNITY_EXECUTABLE_PATH = Path(__file__).parent.parent / "unity" / "Target" / "Win" / "Playground.exe"

def start_unity(wait_time=5):
    """
    启动 Unity 可执行文件，并等待指定的时间确保启动完成。
    """
    try:
        unity_process = subprocess.Popen([str(UNITY_EXECUTABLE_PATH)])
        print("Unity environment started.")
        time.sleep(wait_time)
        return unity_process
    except Exception as e:
        print(f"Failed to start Unity environment: {e}")
        return None

def stop_unity(unity_process):
    """
    关闭 Unity 环境。
    """
    if unity_process and unity_process.poll() is None:
        unity_process.terminate()
        print("Unity environment terminated.")
    else:
        print("Unity process is already terminated or does not exist.")
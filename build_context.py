import os
import time

# Quy định quét từ folder Assets gốc để chấp mọi loại folder con
UNITY_SCRIPT_DIR = "./Assets" 
OUTPUT_FILE = "Master_Code_Context.txt"

def generate_master_context():
    print("[*] Đang tiến hành quét sâu toàn bộ thư mục Assets...")
    start_time = time.time()
    file_count = 0
    
    with open(OUTPUT_FILE, "w", encoding="utf-8") as outfile:
        outfile.write("==================================================\n")
        outfile.write("UNITY PROJECT SOURCE CODE CONTEXT FOR GEMINI\n")
        outfile.write(f"Generated on: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
        outfile.write("==================================================\n\n")
        
        for root, dirs, files in os.walk(UNITY_SCRIPT_DIR):
            for file in files:
                if file.endswith(".cs"):
                    file_path = os.path.join(root, file)
                    file_count += 1
                    
                    outfile.write(f"\n\n// [FILE_START]: {file}\n")
                    outfile.write(f"// PATH: {file_path}\n")
                    outfile.write("// ------------------------------------------\n\n")
                    
                    try:
                        with open(file_path, "r", encoding="utf-8") as infile:
                            outfile.write(infile.read())
                    except Exception as e:
                        print(f"[!] Không thể đọc file {file}: {e}")
                        
                    outfile.write(f"\n\n// [FILE_END]: {file}\n")
                    outfile.write("// ==========================================\n")
                    
    end_time = time.time()
    print(f"[✓] Thành công! Đã tìm và gom sạch sẽ {file_count} file .cs vào '{OUTPUT_FILE}'!")

if __name__ == "__main__":
    generate_master_context()
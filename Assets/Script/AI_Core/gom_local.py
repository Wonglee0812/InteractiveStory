import os
import time

# Tự động lấy thư mục hiện tại nơi bạn đặt file python này
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))

# Đặt tên file bắt đầu bằng _A để luôn nằm ở đầu danh sách (Alphabetical order)
OUTPUT_FILE_NAME = "_A_Local_Context.txt"
OUTPUT_FILE_PATH = os.path.join(CURRENT_DIR, OUTPUT_FILE_NAME)

def gather_local_context():
    folder_name = os.path.basename(CURRENT_DIR)
    print(f"[*] Đang quét folder hiện tại: {folder_name} ...")
    
    file_count = 0
    
    # Mở file chế độ "w" (Write) để tự động CHÉP ĐÈ nếu file đã tồn tại
    with open(OUTPUT_FILE_PATH, "w", encoding="utf-8") as outfile:
        outfile.write("==================================================\n")
        outfile.write(f"LOCAL FOLDER CONTEXT: {folder_name}\n")
        outfile.write(f"Generated on: {time.strftime('%Y-%m-%d %H:%M:%S')}\n")
        outfile.write("==================================================\n\n")
        
        # os.walk quét cả folder hiện tại và các folder con bên trong nó
        for root, dirs, files in os.walk(CURRENT_DIR):
            for file in files:
                if file.endswith(".cs"):
                    file_path = os.path.join(root, file)
                    file_count += 1
                    
                    # Lấy đường dẫn tương đối cho gọn gàng dễ đọc
                    rel_path = os.path.relpath(file_path, CURRENT_DIR)
                    
                    outfile.write(f"\n\n// [FILE_START]: {file}\n")
                    outfile.write(f"// PATH: {rel_path}\n")
                    outfile.write("// ------------------------------------------\n\n")
                    
                    try:
                        with open(file_path, "r", encoding="utf-8") as infile:
                            outfile.write(infile.read())
                    except Exception as e:
                        print(f"[!] Lỗi khi đọc file {file}: {e}")
                        
                    outfile.write(f"\n\n// [FILE_END]: {file}\n")
                    outfile.write("// ==========================================\n")
                    
    print(f"[✓] Ngon lành! Đã gom {file_count} file .cs vào '{OUTPUT_FILE_NAME}'")
    print(f"[*] File xuất ra đang nằm ngay trên cùng của folder này.")

if __name__ == "__main__":
    gather_local_context()
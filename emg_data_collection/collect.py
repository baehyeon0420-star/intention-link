import serial, csv, time

PORT = "/dev/cu.usbserial-1120"
SUBJECT = "s4"     # 사람마다 s1, s2, s3, s4
GESTURE = "grip"   # rest / grip 등 라벨

ser = serial.Serial(PORT, 115200, timeout=1)
time.sleep(2)  # ESP32 리셋 대기

fname = f"{SUBJECT}_{GESTURE}.csv"
with open(fname, "w", newline="") as f:
    w = csv.writer(f)
    w.writerow(["t_ms", "raw", "subject", "gesture"])
    print("수집 시작, Ctrl+C로 종료")
    try:
        while True:
            line = ser.readline().decode().strip()
            if "," in line:
                t, raw = line.split(",")
                w.writerow([t, raw, SUBJECT, GESTURE])
    except KeyboardInterrupt:
        print(f"저장됨: {fname}")
ser.close()

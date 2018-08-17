from osu_parser.beatmap import Beatmap
from osu.ctb.difficulty import Difficulty
from ppCalc import calculate_pp
import os
import threading
import socket
import struct
import sys
import traceback
import time

CMD_KEEP_ALIVE = 0
CMD_KEEP_ALIVE_OK = 1
CMD_CALCULATE_CTB = 2

TIMEOUT = 3

def process_exist(imagename):
    p = os.popen('tasklist /FI "IMAGENAME eq %s"' % imagename).read()
    if imagename in p:
        return True
    return False

def check_process_exist():
    if not process_exist('Sync.exe'):
        os._exit(0)
    timer = threading.Timer(3, check_process_exist)
    timer.start()

timer = threading.Timer(3, check_process_exist)
timer.start()

def read_string(sock,count,encoding='utf-8'):
    total_bytes = b""
    while True:
        recv_bytes = sock.recv(count - len(total_bytes))
        total_bytes += recv_bytes
        if len(total_bytes) >= count:
            break
    return total_bytes.decode(encoding)

def process_tcp(sock):
    """
    content_count - 4 bytes (int)
    content - string (string)
    mods - 4 bytes (int)
    """
    try:
        while True:
            cmd_bytes = sock.recv(4)
            cmd = int.from_bytes(cmd_bytes,byteorder="little")
            if cmd == CMD_KEEP_ALIVE:
                sock.send(struct.pack("<i",CMD_KEEP_ALIVE_OK))
                continue

            content_count_bytes = sock.recv(4)
            content_count = int.from_bytes(content_count_bytes,byteorder="little")

            content = read_string(sock,content_count)

            mods_bytes = sock.recv(4)
            mods = int.from_bytes(mods_bytes,byteorder="little")

            beatmap = Beatmap(content)
            difficulty = Difficulty(beatmap, mods)

            send_ctb_result(sock,beatmap,difficulty)
    except Exception as identifier:
        traceback.print_exc()
        print("[ERROR]%s" % identifier,file=sys.stderr)
    finally:
        sock.close()


def send_ctb_result(sock,beatmap,difficulty):
    """
    stars - 8 bytes (double)
    pp - 8 bytes (double)
    full_combo 4 bytes (int)
    ar - 8 bytes (double)
    """
    stars = difficulty.star_rating
    
    sock.send(struct.pack("<d", stars))
    sock.send(struct.pack("<i", beatmap.max_combo))
    sock.send(struct.pack("<d", difficulty.beatmap.difficulty["ApproachRate"]))

quit_self = False
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

s.bind(('127.0.0.1', 11800))
s.listen(5)

while not quit_self:
    sock,addr = s.accept()
    sock.settimeout(TIMEOUT)
    t = threading.Thread(target=process_tcp,args=(sock,))
    t.start()

s.close()
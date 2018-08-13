from osu_parser.beatmap import Beatmap
from osu.ctb.difficulty import Difficulty
from ppCalc import calculate_pp
import signal
import socket
import struct
import sys
import threading
import traceback
import time

KEEP_SERVER_RUN = 0
GET_PP = 1

TIMEOUT = 3

def read_string(sock,count,encoding='utf-8'):
    total_bytes = b""
    while True:
        recv_bytes = sock.recv(count)
        total_bytes += recv_bytes
        if len(total_bytes) >= count:
            break
    return total_bytes.decode(encoding)

def process_tcp(sock):
    """
    content_count - 4 bytes (int)
    content - string (string)
    mods - 4 bytes (int)
    max_combo - 4 bytes (int)
    miss - 4 bytes (int)
    accuracy - 8 bytes (double)
    """
    content_count_bytes = sock.recv(4)
    content_count = int.from_bytes(content_count_bytes,byteorder="little")
    s = time.clock()
    content = read_string(sock,content_count)
    print("read string: %f s" % (time.clock() - s))

    mods_bytes = sock.recv(4)
    mods = int.from_bytes(mods_bytes,byteorder="little")

    max_combo_bytes = sock.recv(4)
    max_combo = int.from_bytes(max_combo_bytes,byteorder="little")

    miss_bytes = sock.recv(4)
    miss = int.from_bytes(miss_bytes,byteorder="little")

    accuracy_bytes = sock.recv(8)
    accuracy, = struct.unpack('<d', accuracy_bytes)

    s = time.clock()
    beatmap = Beatmap(content)
    difficulty = Difficulty(beatmap, mods)
    print("get pp: %f s" % (time.clock() - s))

    send_ctb_result(sock,beatmap,difficulty,max_combo,miss,accuracy)

def send_ctb_result(sock,beatmap,difficulty,max_combo,miss,accuracy):
    """
    stars - 8 bytes (double)
    pp - 8 bytes (double)
    full_combo 4 bytes (int)
    """
    stars = difficulty.star_rating
    full_combo = beatmap.max_combo

    if max_combo == 2147483647:
        max_combo = full_combo

    pp = calculate_pp(difficulty, accuracy, max_combo, miss)

    sock.send(struct.pack("<d", stars))
    sock.send(struct.pack("<d", pp))
    sock.send(struct.pack("<i", full_combo))

quit_self = False
s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

s.bind(('127.0.0.1', 11800))
s.listen(5)
s.settimeout(TIMEOUT)

while not quit_self:
    sock,addr = s.accept()

    cmd_type = int.from_bytes(sock.recv(4),byteorder="little")
    if cmd_type == KEEP_SERVER_RUN:
        sock.close()
        continue

    try:
        process_tcp(sock)
    except Exception as identifier:
        traceback.print_exc()
        print("[ERROR]Type:%d" % cmd_type,file=sys.stderr)
        print("[ERROR]%s" % identifier,file=sys.stderr)
    finally:
        sock.close()
s.close()
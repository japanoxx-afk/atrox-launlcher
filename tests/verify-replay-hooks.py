"""Execute trampoline paths, including absent DLL/export, against real x86 code."""
from pathlib import Path
import struct
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *

image = (Path(__file__).resolve().parent.parent / 'artifacts/replay-patched.bin').read_bytes()
sites = [(0x4630e0, 0x4630e6, 1), (0x463223, 0x2000000, 2),
         (0x4b8a60, 0x4b8a65, 1), (0x4b8ef9, 0x4b8f00, 2), (0x4c7271, 0x4c7277, 0)]
for site, end, event in sites:
    for mode in ('present', 'load', 'missing', 'noexport'):
        u = Uc(UC_ARCH_X86, UC_MODE_32)
        u.mem_map(0x400000, 0xb00000); u.mem_write(0x400000, image)
        u.mem_map(0x1000000, 0x10000); u.mem_map(0x2000000, 0x10000)
        for reg, value in [(UC_X86_REG_ESP, 0x1008000), (UC_X86_REG_EAX, 11), (UC_X86_REG_EBX, 22),
                           (UC_X86_REG_ECX, 33), (UC_X86_REG_EDX, 44), (UC_X86_REG_ESI, 55), (UC_X86_REG_EDI, 66), (UC_X86_REG_EBP, 77)]:
            u.reg_write(reg, value)
        u.mem_write(0x1008000, struct.pack('<8I', 66, 11, 22, 33, 44, 0x2000000, 0, 0))
        # exit hook pops EDI and local storage before returning to the caller.
        if site == 0x463223: u.mem_write(0x1008014, struct.pack('<I', end))
        calls = []
        apis = {0xe66e58:(0x2000100, 4), 0xe66e64:(0x2000200, 4), 0xe66e68:(0x2000300, 8), 0xe66e90:(0x2000400, 20)}
        for iat,(addr,pop) in apis.items(): u.mem_write(iat, struct.pack('<I',addr))
        def step(uc, address, size, data):
            if address == end: uc.emu_stop(); return
            if address not in (0x2000100, 0x2000200, 0x2000300, 0x2000400, 0x2000500): return
            sp = uc.reg_read(UC_X86_REG_ESP)
            ret = struct.unpack('<I', uc.mem_read(sp,4))[0]
            pop = {0x2000100:4,0x2000200:4,0x2000300:8,0x2000400:20,0x2000500:4}[address]
            result = 0x1234
            if address == 0x2000100 and mode != 'present': result = 0
            if address == 0x2000200 and mode == 'missing': result = 0
            if address == 0x2000300: result = 0 if mode == 'noexport' else 0x2000500
            if address == 0x2000400: result = 1
            if address == 0x2000500: calls.append(struct.unpack('<I',uc.mem_read(sp+4,4))[0])
            uc.reg_write(UC_X86_REG_EAX,result)
            uc.reg_write(UC_X86_REG_ECX,0xdead); uc.reg_write(UC_X86_REG_EDX,0xbeef)
            uc.reg_write(UC_X86_REG_ESP,sp+4+pop); uc.reg_write(UC_X86_REG_EIP,ret)
        u.hook_add(UC_HOOK_CODE,step)
        u.emu_start(site, 0, count=500)
        assert u.reg_read(UC_X86_REG_EIP)==end,(hex(site),mode,'bad return')
        assert calls == ([event] if mode in ('present','load') else []),(hex(site),mode,calls)
        assert u.reg_read(UC_X86_REG_EAX)==(1 if event==0 else 11)
print('20 replay trampoline paths passed (all hooks, DLL load/failure, missing export).')

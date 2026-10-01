"""Run every gameplay trampoline's original/override/missing-module path."""
from pathlib import Path
import struct
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
image = (Path(__file__).resolve().parent.parent / 'artifacts/replay-patched.bin').read_bytes()
for event, (site, length, pop) in enumerate(((0x503f90,7,8),(0x43d850,6,4),(0x46b8e0,5,0))):
    for mode in ('present','fallback','load','missing','noexport'):
        u=Uc(UC_ARCH_X86,UC_MODE_32)
        u.mem_map(0x400000,0xb00000);u.mem_write(0x400000,image)
        u.mem_map(0x1000000,0x10000);u.mem_map(0x2000000,0x10000)
        sp=0x1008000; context=0x1001000; returned=0x2000000
        u.mem_write(sp,struct.pack('<3I',returned,0x1234,0x5678))
        u.mem_write(context+4,struct.pack('<I',0x6789))
        regs={UC_X86_REG_EAX:11,UC_X86_REG_EBX:22,UC_X86_REG_ECX:context,UC_X86_REG_EDX:44,
              UC_X86_REG_ESI:55,UC_X86_REG_EDI:66,UC_X86_REG_EBP:77,UC_X86_REG_ESP:sp}
        for r,v in regs.items():u.reg_write(r,v)
        for iat,target in ((0xe66e58,0x2000100),(0xe66e64,0x2000200),(0xe66e68,0x2000300)):
            u.mem_write(iat,struct.pack('<I',target))
        calls=[]
        def hook(uc,a,size,data):
            if a in (site+length,returned):uc.emu_stop();return
            if a not in (0x2000100,0x2000200,0x2000300,0x2000400):return
            s=uc.reg_read(UC_X86_REG_ESP);ret=struct.unpack('<I',uc.mem_read(s,4))[0]
            consumed={0x2000100:4,0x2000200:4,0x2000300:8,0x2000400:12}[a]
            result=0x123
            if a==0x2000100 and mode not in ('present','fallback'):result=0
            if a==0x2000200 and mode=='missing':result=0
            if a==0x2000300:result=0 if mode=='noexport' else 0x2000400
            if a==0x2000400:
                args=struct.unpack('<3I',uc.mem_read(s+4,12));calls.append(args)
                assert args==(event,context,sp+4 if event==0 else 0x1234 if event==1 else 0),args
                result=0 if mode=='fallback' else 1
            uc.reg_write(UC_X86_REG_EAX,result);uc.reg_write(UC_X86_REG_ECX,0xdead);uc.reg_write(UC_X86_REG_EDX,0xbeef)
            uc.reg_write(UC_X86_REG_ESP,s+4+consumed);uc.reg_write(UC_X86_REG_EIP,ret)
        u.hook_add(UC_HOOK_CODE,hook);u.emu_start(site,0,count=500)
        handled=event<2 and mode in ('present','load')
        assert u.reg_read(UC_X86_REG_EIP)==(returned if handled else site+length),(event,mode)
        assert bool(calls)==(mode in ('present','fallback','load'))
        if handled:
            assert u.reg_read(UC_X86_REG_ESP)==sp+4+pop
            assert u.reg_read(UC_X86_REG_EAX)==1
        else:
            assert u.reg_read(UC_X86_REG_ESP)==sp-(0 if event==0 else 0xa0 if event==1 else 0x60)
            assert u.reg_read(UC_X86_REG_EAX)==11
        for r in (UC_X86_REG_EBX,UC_X86_REG_ESI,UC_X86_REG_EDI,UC_X86_REG_EBP):assert u.reg_read(r)==regs[r]
print('15 gameplay trampoline paths passed: arguments, fallback, registers and stack cleanup.')

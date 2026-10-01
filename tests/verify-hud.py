from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
import struct
code=open('artifacts/hud-clear.bin','rb').read()
u=Uc(UC_ARCH_X86,UC_MODE_32)
u.mem_map(0x400000,0x100000); u.mem_map(0xb27000,0x1000); u.mem_map(0x1000000,0x10000)
u.mem_write(0x463610,code);u.mem_write(0x405e43,b'\xc2\x18\x00');u.mem_write(0xb27730,struct.pack('<I',0x12345678))
regs=[UC_X86_REG_EAX,UC_X86_REG_EBX,UC_X86_REG_ECX,UC_X86_REG_EDX,UC_X86_REG_ESI,UC_X86_REG_EDI,UC_X86_REG_EBP]
for i,r in enumerate(regs):u.reg_write(r,0x100+i)
u.reg_write(UC_X86_REG_ESP,0x1008000);u.reg_write(UC_X86_REG_EFLAGS,0x246)
calls=[]
def hook(u,a,n,d):
 if a==0x405e43:
  sp=u.reg_read(UC_X86_REG_ESP)
  calls.append((u.reg_read(UC_X86_REG_ECX),struct.unpack('<6I',u.mem_read(sp+4,24))))
u.hook_add(UC_HOOK_CODE,hook);u.emu_start(0x463610,0x46320f,count=1000)
assert calls==[(0xde8cf0,(0,0,916,239,107,0)),(0xde8cf0,(0,1040,916,239,107,0))],calls
for i,r in enumerate(regs):assert u.reg_read(r)==(0x12345678 if r==UC_X86_REG_ECX else 0x100+i)
assert u.reg_read(UC_X86_REG_ESP)==0x1008000
assert u.reg_read(UC_X86_REG_EFLAGS)==0x246
print('x86 emulation passed: both HUD fills, balanced stack, preserved flags/registers, resumed UI drawing.')


"""Check saved-load hook register preservation and partial UI cleanup."""
from pathlib import Path
import struct
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
image=(Path(__file__).resolve().parent.parent/'artifacts/replay-patched.bin').read_bytes()
def machine():
 u=Uc(UC_ARCH_X86,UC_MODE_32);u.mem_map(0x400000,0xb00000);u.mem_write(0x400000,image)
 u.mem_map(0x1000000,0x10000);u.mem_map(0x2000000,0x10000)
 u.reg_write(UC_X86_REG_ESP,0x1008000)
 return u
for mode in ('present','load','missing','noexport'):
 u=machine();sp=0x1008000
 for iat,target in ((0xe66e58,0x2000100),(0xe66e64,0x2000200),(0xe66e68,0x2000300)):
  u.mem_write(iat,struct.pack('<I',target))
 regs={UC_X86_REG_EAX:11,UC_X86_REG_EBX:22,UC_X86_REG_EDX:44,UC_X86_REG_ESI:55,UC_X86_REG_EDI:66,UC_X86_REG_EBP:77}
 for r,v in regs.items():u.reg_write(r,v)
 u.mem_write(0xb27708,struct.pack('<I',0x123456))
 called=[]
 def hook(uc,a,size,data):
  if a==0x48b479:uc.emu_stop();return
  if a not in (0x2000100,0x2000200,0x2000300,0x2000400):return
  s=uc.reg_read(UC_X86_REG_ESP);ret=struct.unpack('<I',uc.mem_read(s,4))[0]
  result=1;consumed={0x2000100:4,0x2000200:4,0x2000300:8,0x2000400:0}[a]
  if a==0x2000100 and mode!='present':result=0
  if a==0x2000200 and mode=='missing':result=0
  if a==0x2000300:result=0 if mode=='noexport' else 0x2000400
  if a==0x2000400:called.append(1)
  uc.reg_write(UC_X86_REG_EAX,result);uc.reg_write(UC_X86_REG_ECX,0xdead);uc.reg_write(UC_X86_REG_EDX,0xbeef)
  uc.reg_write(UC_X86_REG_ESP,s+4+consumed);uc.reg_write(UC_X86_REG_EIP,ret)
 u.hook_add(UC_HOOK_CODE,hook);u.emu_start(0x48b473,0,count=200)
 assert u.reg_read(UC_X86_REG_EIP)==0x48b479
 assert bool(called)==(mode in ('present','load'))
 assert u.reg_read(UC_X86_REG_ESP)==sp and u.reg_read(UC_X86_REG_ECX)==0x123456
 for r,v in regs.items():assert u.reg_read(r)==v
for mode in ('null','uninitialized','empty','populated'):
 u=machine();obj=0 if mode=='null' else 0x1001000;u.reg_write(UC_X86_REG_ECX,obj)
 u.mem_write(0x1008000,struct.pack('<I',0x2000000))
 if mode=='empty':u.mem_write(obj+0x3e,b'\xff\xff')
 if mode=='populated':u.mem_write(obj+0x68,struct.pack('<I',0x1002000))
 def stop(uc,a,size,data):
  if a in (0x418738,0x2000000):uc.emu_stop()
 u.hook_add(UC_HOOK_CODE,stop);u.emu_start(0x418730,0,count=100)
 skipped=mode in ('null','uninitialized')
 assert u.reg_read(UC_X86_REG_EIP)==(0x2000000 if skipped else 0x418738)
 assert u.reg_read(UC_X86_REG_ESP)==0x1008000+(4 if skipped else -8)
print('8 saved-load/cleanup hook paths passed.')

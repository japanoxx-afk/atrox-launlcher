"""Execute the scroll hook and both original input paths in an x86 emulator."""
from pathlib import Path
import struct
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
def signed(v): return struct.unpack('<i', struct.pack('<I',v))[0]
def scale(v,p): return (max(1,abs(v)*p//100)*(1 if v>0 else -1)) if v else 0
for percent in [5,10,20,40,60,80,100]:
 code=Path(f'artifacts/scroll-{percent}.bin').read_bytes()
 for x in [-140,-20,-1,0,1,20,140]:
  for y in [-140,-20,-1,0,1,20,140]:
   u=Uc(UC_ARCH_X86,UC_MODE_32);u.mem_map(0x400000,0x100000);u.mem_map(0x1000000,0x10000)
   u.mem_write(0x42d2b0,code);u.mem_write(0x40727a,b'\x8b\x44\x24\x04\xc2\x04\x00')
   u.mem_write(0x1008018,struct.pack('<I',0x12345678));u.reg_write(UC_X86_REG_ESP,0x1008000)
   for r,v in [(UC_X86_REG_ESI,x),(UC_X86_REG_EBX,y),(UC_X86_REG_EDX,777),(UC_X86_REG_EDI,888),(UC_X86_REG_EBP,999)]:u.reg_write(r,v&0xffffffff)
   u.emu_start(0x42d2b0,0x42d47d,count=100)
   assert signed(u.reg_read(UC_X86_REG_EAX))==scale(x,percent),(x,percent)
   assert signed(u.reg_read(UC_X86_REG_EBX))==scale(y,percent),(y,percent)
   assert u.reg_read(UC_X86_REG_ECX)==0x12345678
   assert u.reg_read(UC_X86_REG_ESP)==0x1008000
   assert u.reg_read(UC_X86_REG_EDX)==777 and u.reg_read(UC_X86_REG_EDI)==888 and u.reg_read(UC_X86_REG_EBP)==999
print('343 x86 scroll cases passed: all rates, both axes, signed directions, zero/minimum movement, boundary call, stack/register preservation.')
# Exercise the real function, including the branch that skipped the previous patch.
template=Path.home()/'Downloads/AtroxLauncher/Atrox.ex_'
if template.exists():
 original=template.read_bytes()[0x2d330:0x2d4b5]
 for bypass in [0,1]:
  u=Uc(UC_ARCH_X86,UC_MODE_32);u.mem_map(0x400000,0x100000);u.mem_map(0xb00000,0x100000);u.mem_map(0x1000000,0x200000)
  u.mem_write(0x42d330,original);u.mem_write(0x42d2b0,Path('artifacts/scroll-10.bin').read_bytes())
  u.mem_write(0x42d473,b'\xe9'+struct.pack('<i',0x42d2b0-0x42d478)+b'\x90'*5)
  u.mem_write(0x4015d7,b'\xb8'+struct.pack('<I',0x1100000)+b'\xc3')
  for a in [0x40727a,0x402414]:u.mem_write(a,b'\x8b\x44\x24\x04\xc2\x04\x00')
  u.mem_write(0x406dd9,b'\xc2\x08\x00')
  u.mem_write(0xb27704,struct.pack('<I',0xb00000));u.mem_write(0xbcdaa0,bytes([bypass]));u.mem_write(0xb27708,struct.pack('<I',0x1120000))
  for off,v in [(0x10,1),(0x14,1),(0x18,0),(0x1c,0),(0x20,100),(0x24,-100),(0x34,4),(0x38,4)]:u.mem_write(0x1100000+off,struct.pack('<i',v))
  u.reg_write(UC_X86_REG_ESP,0x1008000);u.mem_write(0x1008000,struct.pack('<I',0x400100));calls=[]
  def hook(u,a,n,d):
   if a in [0x40727a,0x402414]:calls.append(struct.unpack('<i',u.mem_read(u.reg_read(UC_X86_REG_ESP)+4,4))[0])
  u.hook_add(UC_HOOK_CODE,hook);u.emu_start(0x42d330,0x400100,count=1000)
  assert calls==[10,-10],(bypass,calls)
 print('Real game function: normal and bypass input paths both reduced to 10%, X/Y boundary calls preserved.')

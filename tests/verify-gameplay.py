from pathlib import Path
import struct
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
p=lambda v:struct.pack('<I',v&0xffffffff)
code=Path('artifacts/resource-rally.bin').read_bytes()
# Concrete vtables from the original constructors, including non-worker base
# classes that share the race flags used incorrectly by v1.3.5.
workers = {0x5e4ef0: 'Ozzy', 0x5e34f4: 'Nailer', 0x5e082c: 'Engineer'}
cases = [(vt, target, mineral, inactive) for vt in workers
         for target, mineral, inactive in [(1,1,0),(0,0,0),(1,0,0),(1,1,1)]]
cases += [(0x5e43e8,1,1,0),(0x5e46d4,1,1,0)]
for table,target,mineral,inactive in cases:
 worker = table in workers
 u=Uc(UC_ARCH_X86,UC_MODE_32);u.mem_map(0x400000,0x300000);u.mem_map(0xb27000,0x1000);u.mem_map(0x1000000,0x20000)
 u.mem_write(0x414160,code);u.mem_write(0x40517d,b'\xb8'+p(12 if target else 0)+b'\xc3');u.mem_write(0x5946b7,b'\xb8'+p(0x1003000 if mineral else 0)+b'\xc3')
 unit=0x1000000;cmd=0x1002000;sp=0x1018000
 original=struct.pack('<6I',0x36,1,0,1000,1200,0);u.mem_write(cmd,original)
 for a,v in [(unit,table),(unit+4,0x1110b if worker else 0x110b),(unit+0x3ac,0x1004000),(0x1004004,2),(table+0xa0,0x400100),(0xb27700,0x1005000),(0x1005008,0x1006000),(0x1006000+12*4,0x1003000),(0x1003104,inactive),(sp,0x50411d),(sp+4,cmd)]:u.mem_write(a,p(v))
 u.reg_write(UC_X86_REG_ESP,sp);u.reg_write(UC_X86_REG_ECX,unit);u.reg_write(UC_X86_REG_EAX,table);u.reg_write(UC_X86_REG_EBX,0x888);u.reg_write(UC_X86_REG_EDX,1200);u.reg_write(UC_X86_REG_EFLAGS,0x246)
 calls=[]
 def hook(u,a,n,d):
  if a==0x40517d:calls.append(struct.unpack('<3I',u.mem_read(u.reg_read(UC_X86_REG_ESP)+4,12)))
 u.hook_add(UC_HOOK_CODE,hook);u.emu_start(0x414160,0x400100,count=200)
 actual=bytes(u.mem_read(cmd,24))
 expected=struct.pack('<6I',0x3a,2,0,12,1200,0) if worker and target and mineral and not inactive else original
 assert actual==expected,(worker,target,mineral,inactive,actual)
 assert calls==([(2,1000,1200)] if worker else [])
 assert u.reg_read(UC_X86_REG_ESP)==sp and u.reg_read(UC_X86_REG_ECX)==unit and u.reg_read(UC_X86_REG_EAX)==table
 assert u.reg_read(UC_X86_REG_EBX)==0x888 and u.reg_read(UC_X86_REG_EDX)==1200 and u.reg_read(UC_X86_REG_EFLAGS)==0x246
print('14 rally cases passed: Ozzy/Nailer/Engineer gather; non-workers, ground, other targets and inactive muon keep Move; registers/stack preserved.')
# Run the actual HUD exclusion code with a controlled sprite hit-test result.
b=(Path.home()/'Downloads/AtroxLauncher/Atrox.ex_').read_bytes()
u=Uc(UC_ARCH_X86,UC_MODE_32);u.mem_map(0x400000,0x100000);u.mem_map(0x1000000,0x10000)
u.mem_write(0x41d5ae,b[0x1d5ae:0x1d5ce]);u.mem_write(0x41d5c4,p(1024))
for x in [0,799,800,1040,1279]:
 for y in [0,485,486,522,730,915,940,1023,1024]:
  for sprite_hit in [0,1]:
   u.reg_write(UC_X86_REG_EAX,sprite_hit);u.reg_write(UC_X86_REG_ESP,0x1008000);u.reg_write(UC_X86_REG_EBP,0x1000000)
   u.mem_write(0x1008010,p(x)+p(y));u.mem_write(0x1000031,b'\x7f');u.emu_start(0x41d5ae,0x41d5ce,count=30)
   assert u.mem_read(0x1000031,1)==bytes([int(bool(sprite_hit) or y>=1024)])
print('90 viewport positions passed with and without actual HUD sprite hits, including stationary V/D at Y=522 and expanded right/bottom regions.')

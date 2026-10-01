"""Emulate real terrain composition at every vertical cache position; no game rendering mocked."""
from pathlib import Path
import struct
from unicorn import Uc, UC_ARCH_X86, UC_MODE_32, UC_HOOK_CODE
from unicorn.x86_const import *
p=lambda v:struct.pack('<I',v&0xffffffff)
rows=Path('artifacts/tile-rows.bin').read_bytes();wrap=Path('artifacts/terrain-wrap.bin').read_bytes()
u=Uc(UC_ARCH_X86,UC_MODE_32);u.mem_map(0x400000,0x200000);u.mem_map(0xb26000,0x2000);u.mem_map(0x1000000,0x900000)
u.mem_write(0x463610,rows);u.mem_write(0x463630,wrap)
for height in [400,480,600,944,1024]:
 u.reg_write(UC_X86_REG_EAX,0x66666667);u.reg_write(UC_X86_REG_ECX,height);u.emu_start(0x463610,0x4dc8f8,count=20)
 assert u.reg_read(UC_X86_REG_EDX)==(height+39)//40
print('Tile cache row ceiling verified for original and high resolutions.')
template=Path.home()/'Downloads/AtroxLauncher/Atrox.ex_'
if not template.exists():raise SystemExit('Real compositor verification requires the supported local game template.')
b=template.read_bytes();u.mem_write(0x4b8d4d,b[0xb8d4d:0xb8efe])
u.mem_write(0x4b8e69,b'\xe9'+struct.pack('<i',0x463630-0x4b8e6e)+b'\x90'*5)
u.mem_write(0x4015d7,b'\xb8'+p(0xb26dfc)+b'\xc3');u.mem_write(0x4048b8,b'\xc2\x10\x00');u.mem_write(0x4059bb,b'\xc3')
rects=[]
def hook(u,a,n,d):
 if a==0x4048b8:
  sp=u.reg_read(UC_X86_REG_ESP);x,y,src,dst=struct.unpack('<4I',u.mem_read(sp+4,16))
  rects.append((x,y,struct.unpack('<4i',u.mem_read(src,16))))
u.hook_add(UC_HOOK_CODE,hook)
count=0
for h in [480,944,1024]:
 cache_h=((h+39)//40+1)*40
 for sy in range(cache_h):
  for sx in [0,79,80,1279,1280,1359]:
   rects.clear();sp=0x1808000;model=0x1100000
   u.reg_write(UC_X86_REG_ESP,sp);u.reg_write(UC_X86_REG_ESI,model);u.reg_write(UC_X86_REG_EBX,model+0x35c550)
   for addr,v in [(0xb26dfc+0x64,sx%80),(0xb26dfc+0x68,sy%40),(0xb26dfc+0x7c,1280),(0xb26dfc+0x80,h),(model+0x35c530,sx//80),(model+0x35c534,sy//40),(model+0x35c538,17),(model+0x35c53c,cache_h//40)]:u.mem_write(addr,p(v))
   u.emu_start(0x4b8d4d,0x4b8ef9,count=300)
   area=0;out=[]
   for x,y,(l,t,r,bot) in rects:
    if r<=l or bot<=t:continue
    w,hh=r-l,bot-t
    assert 0<=l<r<=1360 and 0<=t<bot<=cache_h,(h,sx,sy,rects)
    assert x+w<=1280 and y+hh<=h,(h,sx,sy,rects)
    assert (sx+x)%1360==l and (sy+y)%cache_h==t
    for ox,oy,ow,oh in out:assert x>=ox+ow or ox>=x+w or y>=oy+oh or oy>=y+hh
    out.append((x,y,w,hh));area+=w*hh
   assert area==1280*h,(h,sx,sy,rects,area)
   count+=1
print(f'{count} real x86 terrain compositions passed: no gaps, overlap, out-of-bounds copies or incorrect wrap coordinates.')

# Atrox Launcher

게임 경로와 화면 설정을 한 화면에서 관리하는 Windows 런처입니다. 지원하는 `Atrox.ex_` 원본을 확인한 후 게임 실행 파일을 매번 새로 패치합니다. 게임 원본은 이 저장소에서 배포하지 않습니다.

- v1.3.7: 어두운 테마로 화면을 재구성하고 이메일 표기 및 사용자 정의 시나리오 선택·설치 기능을 제거했습니다. JPak 옵션은 유지합니다.
- 1280x1024에서 하단 HUD 양쪽에도 지형과 게임 오브젝트를 그립니다. 지형 캐시 높이와 스크롤 경계 복사를 수정해 검은 가로줄 및 잔상을 방지합니다.
- 확장 해상도의 건설 미리보기 입력 판정을 수정했습니다. 원래 800x600 화면의 하단 경계(Y=486)가 확장 영역을 가리지 않도록 하고, 실제 HUD 위의 입력 제한은 유지합니다.
- 생산 건물의 랠리를 뮤온에 지정하면 오지·네일러·엔지니어에게 게임의 기본 채취 명령을 전달합니다. 일반 유닛이나 자원이 없는 지점은 기존 이동 명령을 유지합니다.
- 화면 이동 속도는 기본 10%이며 5~100%를 선택할 수 있습니다. 속도 변경은 다음 게임 실행에 적용됩니다.
- Alt+Enter로 창 모드와 모니터 전체를 채우는 테두리 없는 전체화면을 전환합니다.
- v1.3.3 최초 실행 시 이전 속도 설정은 10%로 조정됩니다. 이후 선택한 속도는 유지됩니다. 전체화면에서는 가로·세로를 각각 늘리므로 원본 화면 비율이 달라질 수 있습니다.
- 런처 업데이트 버튼은 GitHub Release의 실행 파일과 SHA256을 검증하여 적용합니다.

화면 출력은 MIT 라이선스의 [cnc-ddraw v7.1.0.0](https://github.com/FunkyFr3sh/cnc-ddraw/releases/tag/v7.1.0.0)을 포함합니다. 최초 실행 시 게임 폴더에 `ddraw.dll`, `ddraw.ini`, 라이선스를 설치합니다. 기존 출력 모듈 및 설정은 해시가 포함된 `.before-launcher-*` 파일로 보존합니다. 기존 게임 데이터, 저장 파일과 원본 런처는 유지합니다.

## 빌드와 검증

Windows 및 Visual Studio MSBuild가 필요합니다.

```powershell
./build.ps1
./tests/verify.ps1
```

결과는 `dist/AtroxLauncher.exe`입니다. 검증은 설정 경로 해석, 배포 체크섬, 두 축의 속도 패치, 지형 캐시와 전체 높이 설정 및 원본 코드 불일치 차단을 확인합니다. 선택적으로 x86 명령 실행도 검증할 수 있습니다.

```powershell
$env:ATROX_EXPORT_PATCH = "$PWD\artifacts\terrain.bin"
./tests/verify.ps1
python -m pip install unicorn
python ./tests/verify-terrain.py
python ./tests/verify-scroll.py
python ./tests/verify-gameplay.py
```

명령 실행 검증에는 로컬의 지원 원본(`~/Downloads/AtroxLauncher/Atrox.ex_`)과 Unicorn이 필요합니다. 게임플레이 검증은 HUD 입력 경계의 실제 x86 코드와 랠리 명령 변환을 실행합니다. 랠리 대상 검색 및 RTTI 판정은 모의 함수이므로 실제 게임 내 채취 확인도 필요합니다.

지원 원본 SHA256: `b9561ed32e1c5f4275862b5b2afda5425fd4600735a1179ffbdf9b5ac603d6f5`.

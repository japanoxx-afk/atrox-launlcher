# Atrox Launcher

기존 v1.2 구조를 유지한 Windows 런처입니다. 지원하는 `Atrox.ex_` 원본을 확인한 후 게임 실행 파일을 매번 새로 패치합니다. 게임 원본은 이 저장소에서 배포하지 않습니다.

- 1280x1024에서 하단 HUD 양쪽을 매 프레임 다시 그려 마우스 잔상을 제거합니다.
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

결과는 `dist/AtroxLauncher.exe`입니다. 검증은 설정 경로 해석, 배포 체크섬, 두 축의 속도 패치, 그리기 후크의 복귀 주소 및 원본 코드 불일치 차단을 확인합니다. 선택적으로 x86 명령 실행도 검증할 수 있습니다.

```powershell
$env:ATROX_EXPORT_PATCH = "$PWD\artifacts\hud-clear.bin"
./tests/verify.ps1
python -m pip install unicorn
python ./tests/verify-hud.py
python ./tests/verify-scroll.py
```

지원 원본 SHA256: `b9561ed32e1c5f4275862b5b2afda5425fd4600735a1179ffbdf9b5ac603d6f5`.

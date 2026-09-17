# ESC 설정 메뉴

`GameSettingsMenu.cs`는 씬에 직접 배치할 필요가 없습니다. 게임이 시작되면 자동으로 한 번 생성되며 씬 전환 뒤에도 유지됩니다.

## 기능

- `ESC`: 설정 메뉴 열기/닫기
- 전체 화면 / 창 모드 전환
- 마스터 / BGM / SFX 볼륨 조절
- 확인창이 있는 게임 종료 버튼
- `PlayerPrefs`를 사용한 설정값 저장
- 설정 메뉴를 연 동안 게임 진행 일시정지

## 오디오 분류

AudioSource의 오브젝트 또는 클립 이름에 `BGM`, `Music`, `Theme`, `Ambient`가 포함되거나 `Loop`가 켜져 있으면 BGM으로 자동 분류합니다. 그 외에는 SFX로 분류합니다.

자동 분류가 틀린 AudioSource에는 `GameAudioChannel` 컴포넌트를 추가하고 BGM 또는 SFX를 직접 선택하면 됩니다.

## 되돌리기

아래 두 스크립트를 제거하면 원래 상태로 돌아갑니다. 씬 파일은 수정하지 않았습니다.

- `GameSettingsMenu.cs`
- `GameAudioChannel.cs`

# Unity 화투 UI 에셋 적용 및 값 수정 가이드

이 문서는 현재 제작된 `MathHighLowGame` 씬에 사용자가 준비한 이미지를 넣고, 화면 배치와 게임 값을 직접 변경하는 순서를 설명한다.

현재 프로젝트에는 게임 로직과 버튼 연결이 이미 완료되어 있다. 따라서 스크립트를 다시 연결하기보다는 아래 순서대로 이미지, 색상, 크기와 게임 수치만 교체하는 것을 권장한다.

---

## 1. 작업 전에 확인할 파일

Unity 프로젝트 폴더:

```text
C:\UnityProject\MathHighLow\My project
```

사용할 게임 씬:

```text
Assets/Scenes/MathHighLowGame.unity
```

공통 화투 카드 프리팹:

```text
Assets/Prefabs/EditableCard.prefab
```

주요 게임 설정 오브젝트:

```text
GAME_SYSTEMS
```

UI 편집용 최상위 오브젝트:

```text
GAME_UI__EDIT_IMAGES_AND_LAYOUT_HERE
```

### 중요한 주의 사항

Unity 상단 메뉴의 다음 기능은 씬과 카드 프리팹을 처음 상태로 다시 만든다.

```text
MathHighLow > Rebuild Final Game Scene
```

이미지를 넣거나 배치를 수정한 뒤 이 기능을 실행하면 작업 내용이 덮어써질 수 있다. 에셋 작업을 시작한 뒤에는 실행하지 않거나, 씬과 프리팹을 먼저 복제해 백업한다.

---

## 2. Unity에서 게임 씬 열기

1. Unity에서 `My project` 프로젝트를 연다.
2. Project 창에서 `Assets > Scenes` 폴더를 연다.
3. `MathHighLowGame` 씬을 더블 클릭한다.
4. Hierarchy에서 `GAME_UI__EDIT_IMAGES_AND_LAYOUT_HERE`를 펼친다.
5. Game 창의 화면 비율을 `16:9`로 설정한다.

현재 UI는 1920×1080 비율을 기준으로 배치되어 있다. 다른 비율에서 먼저 작업하면 위치가 다르게 보일 수 있다.

---

## 3. 이미지 파일을 Unity로 가져오기

먼저 이미지 정리용 폴더를 만드는 것을 권장한다.

```text
Assets/Art/Background
Assets/Art/Cards
Assets/Art/UI
Assets/Art/Effects
```

이미지마다 다음 순서로 설정한다.

1. PNG 또는 PSD 파일을 원하는 `Assets/Art` 폴더로 드래그한다.
2. Project 창에서 가져온 이미지를 선택한다.
3. Inspector의 `Texture Type`을 `Sprite (2D and UI)`로 변경한다.
4. 투명 배경 PNG라면 `Alpha Is Transparency`를 켠다.
5. `Apply`를 누른다.

### 권장 이미지 용도

| 이미지 종류 | 권장 형태 |
|---|---|
| 전체 게임 배경 | 16:9, 예: 1920×1080 |
| 숫자·연산자별 화투 카드 앞면 | 세로형, 숫자·기호 포함 PNG |
| 화투 카드 뒷면 | 세로형 |
| 플레이어 손패 | 별도 묶음 이미지 불필요—실제 카드 자동 배치 |
| 결과 화면·한방 승부 연출 | 16:9 또는 중앙 패널형 |

---

## 4. 전체 배경 이미지 넣기

1. Hierarchy에서 다음 오브젝트를 선택한다.

```text
GAME_UI__EDIT_IMAGES_AND_LAYOUT_HERE
└─ ART_SLOT__Background
```

2. Inspector의 `Image` 컴포넌트를 찾는다.
3. 준비한 배경 Sprite를 `Source Image` 칸으로 드래그한다.
4. 배경이 화면을 꽉 채워야 하면 `Preserve Aspect`를 끈다.
5. 원본 비율을 유지해야 하면 `Preserve Aspect`를 켠다.
6. `Color`는 이미지 원색이 보이도록 흰색으로 설정한다.

여기에는 보내준 시안에서 인물, 테이블, 칩이 보이는 전체 배경 이미지를 넣으면 된다.

---

## 5. 화투 카드 앞면과 뒷면 이미지 넣기

모든 숫자·연산자·특수 카드가 사용하는 공통 프리팹을 수정한다.

1. Project 창에서 다음 프리팹을 더블 클릭한다.

```text
Assets/Prefabs/EditableCard.prefab
```

2. Prefab Mode의 Hierarchy에서 다음 오브젝트를 확인한다.

```text
EditableCard
├─ ART_SLOT__Card_Face
├─ ART_SLOT__Card_Back
├─ Card_Value
└─ Card_Type
```

### 5.1 숫자·연산자·특수 카드 앞면 넣기

1. 프리팹의 루트 `EditableCard`를 선택한다.
2. Inspector에서 `Final Card View` 컴포넌트를 찾는다.
3. `Card Face Sprites - Numbers 1~12`를 펼친다.
4. 제작한 숫자 이미지를 같은 번호의 슬롯에 넣는다.

| 이미지 | 넣을 슬롯 |
|---|---|
| 숫자 1 | `Number 1 Sprite` |
| 숫자 2 | `Number 2 Sprite` |
| 숫자 3 | `Number 3 Sprite` |
| 숫자 4 | `Number 4 Sprite` |
| 숫자 5 | `Number 5 Sprite` |
| 숫자 6 | `Number 6 Sprite` |
| 숫자 7 | `Number 7 Sprite` |
| 숫자 8 | `Number 8 Sprite` |
| 숫자 9 | `Number 9 Sprite` |
| 숫자 10 | `Number 10 Sprite` |
| 숫자 11 | `Number 11 Sprite` |
| 숫자 12 | `Number 12 Sprite` |

5. `Card Face Sprites - Operators / Specials`를 펼친다.
6. 각 기호가 그려진 이미지를 다음 슬롯에 넣는다.

| 이미지 | 넣을 슬롯 |
|---|---|
| `+` | `Add Sprite` |
| `-` | `Subtract Sprite` |
| `÷` | `Divide Sprite` |
| `×` | `Multiply Sprite` |
| `√` | `Square Root Sprite` |

게임 중 카드 데이터에 맞는 이미지가 `ART_SLOT__Card_Face`에 자동으로 표시된다. `ART_SLOT__Card_Face`의 `Source Image`에는 별도의 고정 이미지를 넣지 않는다.

### 5.2 카드 뒷면 넣기

1. `ART_SLOT__Card_Back`을 선택한다.
2. `Image > Source Image`에 화투 카드 뒷면 이미지를 넣는다.
3. `Preserve Aspect`를 켠다.
4. `Color`를 흰색으로 설정한다.

이 뒷면은 다음 카드에 자동으로 사용된다.

- AI의 연산자·특수 카드 3장

### 5.3 공용 `?` 카드(unopencard) 넣기

1. 프리팹의 루트 `EditableCard`를 선택한다.
2. Inspector의 `Final Card View` 컴포넌트를 찾는다.
3. `Shared Unopened Card` 항목을 펼친다.
4. 제작한 `unopencard` Sprite를 `Unopened Card Sprite` 슬롯에 넣는다.

이 이미지는 AI와 플레이어가 함께 사용하는 공용 카드가 플레이어 손패에 있을 때 표시된다. 이미지 안에 `?`가 포함되어 있으므로 별도의 물음표 텍스트는 나오지 않는다.

공용 숫자가 공개되면 `unopencard` 이미지는 자동으로 숨겨지고, 공개된 값에 해당하는 `Number 1 Sprite`부터 `Number 12 Sprite` 중 하나가 표시된다.

`Unopened Card Sprite`를 비워두면 일반 `ART_SLOT__Card_Back` 이미지가 예비 이미지로 사용된다.

### 5.4 별도 기호 텍스트

`Card_Value`와 `Card_Type`은 비활성화되어 있다. 카드 이미지에 숫자와 기호가 포함되어 있으므로 게임 중 별도의 숫자·기호·카드 종류 글자는 표시되지 않는다.

두 오브젝트는 이전 구조와의 참조 호환을 위해 프리팹 안에 남아 있다. 다시 활성화할 필요는 없다.

카드 클릭 기능은 루트 `EditableCard`의 `Button`과 `FinalCardView`가 담당한다. 루트 오브젝트의 `Button`이나 `FinalCardView` 컴포넌트는 삭제하지 않는다.

---

## 6. AI 상단 영역에 이미지 넣기

Hierarchy에서 다음 영역을 펼친다.

```text
AI_AREA__Numbers_Visible_Actions_Hidden
```

현재 동작은 다음과 같다.

- `AI_NumberCards__FACE_UP`: AI 숫자 카드 2장을 앞면으로 표시
- `AI_ActionCards__FACE_DOWN`: AI 연산자·특수 카드 3장을 뒷면으로 표시
- `AI_Money_Panel`: AI 보유 금액 표시

### AI 오른쪽 위 화투패 묶음

장식용 `ART_SLOT__AI_Deal_Deck_Fan`은 제거되었다. 상단에는 실제 AI 숫자 카드 2장과 뒷면 연산자 카드 3장만 표시한다.

게임을 실행하면 `EditableCard > ART_SLOT__Card_Back`에 지정한 뒷면 Sprite 7장이 이 영역에 부채꼴로 자동 생성된다. 카드 수, 간격과 회전값은 `FinalGameView.BuildBackFan()`에서 조절할 수 있다.

---

## 7. 플레이어 하단 영역에 이미지 넣기

Hierarchy에서 다음 영역을 펼친다.

```text
PLAYER_AREA
```

현재 동작은 다음과 같다.

- 왼쪽 아래 손패: 숫자 2장, 연산자·특수 카드 3장, 공용 `unopencard` 1장
- `PLAYER_EXPRESSION_CARD_SLOTS__AUTO`: 손패에서 선택한 카드를 순서대로 내려놓는 파란색 패널 영역
- `Player_Money_Panel`: 보유 금액과 베팅액 조작

### 플레이어 왼쪽 아래 실제 손패

1. 다음 오브젝트를 선택한다.

```text
PLAYER_AREA
└─ ART_SLOT__Player_Deal_Deck_Fan
```

이 영역에는 별도의 패 묶음 이미지를 넣지 않는다. 게임 중 실제 플레이어 카드와 `unopencard`가 부채꼴 손패로 자동 생성된다.

손패 카드를 클릭하면 파란색 패널의 `PLAYER_EXPRESSION_CARD_SLOTS__AUTO`에 선택 순서대로 이동한다. 놓인 카드를 다시 클릭하면 그 카드부터 뒤에 놓인 카드까지 손패로 돌아간다. 이 방식으로 `unopencard`를 첫 번째·두 번째·세 번째 숫자 위치 중 원하는 곳에 배치한다.

손패는 최대 6장의 이미지가 서로 구분되도록 약 300px 범위로 펼쳐진다. 기존 플레이어·AI 최종 숫자 박스는 제거되었으며 계산값은 결과 패널의 수식 상세에서 확인한다.

---

## 8. 중앙 화투 더미

1. Hierarchy에서 다음 오브젝트를 찾는다.

```text
HWATU_DRAW_PILE
└─ ART_SLOT__Hwatu_Draw_Pile
```

`ART_SLOT__Hwatu_Draw_Pile`의 `Source Image`에는 별도 더미 이미지를 넣지 않는다. 게임을 실행하면 `EditableCard > ART_SLOT__Card_Back`의 뒷면 Sprite 7장이 조금씩 어긋나게 겹쳐져 자동 생성된다.

이 더미는 장식용이며 카드를 실제로 뽑는 클릭 버튼은 아니다. 카드 수와 간격은 `FinalGameView.BuildBackStack()`에서 조절할 수 있다.

---

## 9. 공용 `unopencard` 배치

플레이어 손패의 `unopencard`는 수식 안에서 공용 숫자의 위치를 정하는 조작용 카드로 유지한다. 실제 값이 들어 있는 별도의 공용카드는 분배 마지막에 중앙 `SEALED_SHARED_CARD_AREA`에 뒷면으로 놓인다.

공개 전 `?` 카드 이미지는 `EditableCard > FinalCardView > Unopened Card Sprite`에 넣는다.

### 중앙 유리잔 에셋 넣기

1. Hierarchy에서 `SEALED_SHARED_CARD_AREA`를 펼친다.
2. `ART_SLOT__Shared_Glass_Cover`를 선택한다.
3. `Image > Source Image`에 유리잔 Sprite를 드래그한다.
4. `Preserve Aspect`를 체크한다.
5. 크기와 위치는 `Rect Transform`에서 조절한다.

슬롯이 보이지 않으면 Unity 상단 메뉴에서 `MathHighLow > Apply Shared Glass Cover Layout`을 한 번 실행한다. 이 작업은 우측 상단 장식용 AI 카드 뭉치도 함께 제거한다.

`FinalGameView > Shared Card Glass Cover`에서 다음 값을 편집할 수 있다.

| 항목 | 의미 |
|---|---|
| Glass Move Duration | 유리잔 하강·상승 시간 |
| Glass Raised Distance | 유리잔이 위로 올라가는 거리 |
| Shared Card Flip Duration | 카드 뒤집기 전체 시간 |

유리잔이 완전히 내려와 있는 동안 실제 공용카드는 숨겨지므로 화면에는 유리잔 에셋만 보인다. 공개할 때는 유리잔 상승 도중 카드 뒷면이 나타나며, 상승이 끝난 다음 앞면으로 천천히 뒤집힌다. 기본 뒤집기 시간은 `0.9초`다.

### 화투더미 카드 분배 연출

모든 분배용 카드는 `EditableCard > ART_SLOT__Card_Back`의 Sprite를 자동 사용한다. 중앙 `ART_SLOT__Hwatu_Draw_Pile`에서 출발해 플레이어 손패, AI 패 또는 중앙 공용패의 지정 위치까지 이동하면서 기본 180도 회전한다.

`FinalGameView > Card Deal Animation`에서 다음 값을 수정한다.

| 항목 | 기본값 | 의미 |
|---|---:|---|
| Card Deal Duration | 0.42 | 카드 한 장의 이동 시간 |
| Card Deal Arc Height | 5 | 거의 직선에 가까운 약한 곡선 높이 |
| Card Deal Rotation | 180 | 이동 중 회전 각도 |

목적지의 실제 카드는 분배 애니메이션이 끝날 때까지 숨겨지므로 앞면이 먼저 보이지 않는다. 마지막 공용카드가 중앙에 도착하면 이어서 유리잔이 위에서 내려온다.

현재 씬과 `EditableCard` 프리팹의 모든 Legacy UI `Text`에는 `Assets/Font/BMYEONSUNG_ttf.ttf`가 적용되어 있다. 새 UI Text에도 일괄 적용하려면 `MathHighLow > Apply BMYEONSUNG Font To All UI Text` 메뉴를 사용한다.

공용 카드 동작:

1. 라운드 시작 시 플레이어 손패에 `unopencard`로 표시
2. 손패에서 클릭해 원하는 숫자 순서에 배치
3. 플레이어와 AI가 모두 수식에 `?`를 한 번 사용
4. AI 선제 베팅에 CALL하고 수식 작성 완료
5. 카드가 공개되며 실제 숫자 `1~12` 표시
6. 양쪽 최종 수식과 결과 계산

---

## 10. 결과 화면용 이미지 넣기

결과 패널은 평소 비활성화되어 있다. 편집할 때 Hierarchy의 오브젝트 왼쪽 체크박스를 잠시 켜서 확인할 수 있다.

### 10.1 “묻고 더블로 가” 이미지

```text
FINAL_SHOWDOWN_PANEL__Editable
└─ ART_SLOT__Final_Showdown
```

1. 패널을 활성화한다.
2. `ART_SLOT__Final_Showdown`을 선택한다.
3. 한방 승부 배경 또는 캐릭터 연출 이미지를 `Source Image`에 넣는다.
4. 편집이 끝나면 패널을 다시 비활성화한다.

### 10.2 최종 승리·패배 이미지

승리와 패배 이미지는 `ART_SLOT__Game_Over`의 `Source Image`에 직접 넣지 않는다. 게임 결과에 따라 자동 교체될 두 개의 Sprite 슬롯에 따로 넣는다.

1. Hierarchy에서 `GAME_UI__EDIT_IMAGES_AND_LAYOUT_HERE`를 선택한다.
2. Inspector에서 `Final Game View` 컴포넌트를 찾는다.
3. `Final Win / Lose Sprites`를 펼친다.
4. 다음과 같이 이미지를 넣는다.

| 결과 | 넣을 슬롯 |
|---|---|
| 플레이어 최종 승리 | `Game Over Win Sprite` |
| 플레이어 최종 패배 | `Game Over Lose Sprite` |

실제 이미지가 출력되는 화면 오브젝트는 다음과 같다.

```text
GAME_OVER_PANEL__Editable
└─ ART_SLOT__Game_Over
```

플레이어가 최종 승리하면 `Game Over Win Sprite`, 최종 패배하면 `Game Over Lose Sprite`가 `ART_SLOT__Game_Over`에 자동 표시된다. 제목도 각각 `최종 승리`, `최종 패배`로 바뀐다.

---

## 11. 버튼 이미지를 바꾸는 방법

버튼 오브젝트에는 `Button`과 `Image` 컴포넌트가 함께 있다.

예:

```text
Reset_Button
Submit_Button
All_In_Button
Bet_Minus
Bet_Plus
Bet_Confirm_Button
Confirm_All_In
Cancel_All_In
Start_Final_Showdown
```

버튼 이미지 변경 순서:

1. 원하는 버튼 오브젝트를 선택한다.
2. `Image > Source Image`에 버튼 Sprite를 넣는다.
3. `Image Type`을 이미지 제작 방식에 따라 `Simple` 또는 `Sliced`로 설정한다.
4. 버튼 자식의 `Label`을 선택해 글꼴·크기·색상을 조절한다.
5. `Button` 컴포넌트의 `On Click()` 연결은 삭제하지 않는다.

### 현재 동작하는 베팅 버튼

- `OPTION_SLOT__Call_Future`: `받기(CALL)`로 사용. AI와 같은 금액을 걸고 수식 작성으로 이동
- `OPTION_SLOT__Die_Future`: `다이(FOLD)`로 사용. 참가금 1억을 포기하고 라운드 종료

`Bet_Amount`에는 AI가 먼저 건 금액이 `AI BET`으로 표시된다. 플레이어가 금액을 직접 증감하지는 않는다.

### 현재 비활성 버튼

- `OPTION_SLOT__Check_Future`
- `Bet_Minus`
- `Bet_Plus`
- `Bet_Confirm_Button`
- `All_In_Button`

이 세 버튼은 보내준 시안의 위치를 보존하기 위한 미래 확장 슬롯이다. 현재 확정 규칙에는 연결되어 있지 않다.

---

## 12. UI 위치와 크기 수정하기

1. Hierarchy에서 이동할 오브젝트를 선택한다.
2. Inspector의 `Rect Transform`을 수정한다.
3. `Pos X`, `Pos Y`로 위치를 조절한다.
4. `Width`, `Height`로 크기를 조절한다.
5. 부모 영역에 맞는 Anchor가 유지되는지 확인한다.

### 배치 변경 시 권장 원칙

- 실제 카드가 생성되는 컨테이너는 삭제하지 않는다.
- 컨테이너 내부에 임의의 카드 복사본을 넣지 않는다.
- 장식 이미지는 `Raycast Target`을 끈다.
- 카드 위를 덮는 이미지는 Hierarchy 순서와 클릭 차단 여부를 확인한다.
- 텍스트나 장식만 움직이고 싶으면 부모 컨테이너가 아닌 해당 자식 오브젝트를 이동한다.

### 삭제하거나 이름을 바꾸지 않는 것이 좋은 컨테이너

```text
Player_NumberCards
Player_ActionCards
AI_NumberCards__FACE_UP
AI_ActionCards__FACE_DOWN
SHARED_CARD__FACE_DOWN_UNTIL_REVEAL
BETTING_PANEL__Enabled_After_Expression
ALL_IN_CONFIRMATION__Editable
RESULT_PANEL__Editable
FINAL_SHOWDOWN_PANEL__Editable
GAME_OVER_PANEL__Editable
```

이름 자체보다 Inspector 연결이 더 중요하지만, 찾기 쉽도록 현재 이름을 유지하는 것이 안전하다.

---

## 13. 게임 시간과 금액 수정하기

1. Hierarchy에서 `GAME_SYSTEMS`를 선택한다.
2. Inspector에서 `Final Game Controller` 컴포넌트를 찾는다.
3. 원하는 값을 수정한다.

### 시간 설정

| Inspector 항목 | 현재값 | 의미 |
|---|---:|---|
| Submission Unlock Time | 30 | 수식 제출 버튼이 활성화되는 시간(초) |
| Round Duration | 180 | 라운드 전체 제한 시간(초) |
| Results Duration | 10 | 일반 결과 화면 유지 시간(초) |
| Deal Interval | 0.15 | 카드가 한 장씩 나오는 간격(초) |

### 금액 설정

| Inspector 항목 | 현재값 | 게임 내 의미 |
|---|---:|---|
| Starting Credits | 2000000000 | 각자 시작 금액 20억 |
| Min Bet | 100000000 | 최소 베팅 1억 |
| Max Base Bet | 500000000 | 일반 최대 베팅 5억 |
| Bet Step | 100000000 | `+`, `-` 한 번당 1억 |

### AI 베팅 조정값

`GameController`의 `AI Betting` 항목에서 직접 조절할 수 있다.

| Inspector 항목 | 기본값 | 의미 |
|---|---:|---|
| Top Hand Max Average Distance | 1 | 최상급 패로 판단하는 평균 목표 거리 최대값 |
| Middle Hand Max Average Distance | 3 | 중급 패로 판단하는 평균 목표 거리 최대값 |
| Top Hand Strong Bet Chance | 0.7 | 최상급 패가 4억~5억을 거는 확률 70% |
| Middle Hand Normal Bet Chance | 0.8 | 중급 패가 2억~3억을 거는 확률 80% |
| Low Hand Bluff Chance | 0.1 | 하급 패가 5억 블러핑을 하는 확률 10% |

금액은 원 단위로 입력한다. 쉼표 없이 숫자만 입력한다.

예:

```text
20억 = 2000000000
5억  = 500000000
1억  = 100000000
```

현재 Call/Fold 규칙에서는 올인을 사용하지 않는다. `Min Bet` 1억은 FOLD 시 플레이어가 포기하는 참가금으로도 사용된다.

### 숫자 카드 설정

| Inspector 항목 | 현재값 | 의미 |
|---|---:|---|
| Min Number | 1 | 가장 작은 숫자 카드 |
| Max Number | 12 | 가장 큰 숫자 카드 |
| Copies Per Number | 4 | 숫자별 덱 포함 장수 |

숫자 범위를 바꾸면 AI 계산과 공용 카드도 같은 범위를 사용한다. 확정 규칙을 유지하려면 `1~12`를 그대로 둔다.

### 반드시 Inspector에서 바꿔야 하는 이유

스크립트 파일의 기본값을 수정해도 이미 씬에 저장된 컴포넌트 값은 자동으로 바뀌지 않을 수 있다. 현재 게임 값을 바꿀 때는 `GAME_SYSTEMS > FinalGameController`의 Inspector 값을 직접 수정한다.

---

## 14. 색상과 글꼴 수정하기

글자 오브젝트를 선택하고 `Text` 컴포넌트에서 다음 값을 바꾼다.

- `Font`: 사용할 한글 폰트
- `Font Size`: 글자 크기
- `Color`: 글자 색상
- `Alignment`: 정렬
- `Horizontal Overflow`, `Vertical Overflow`: 글자 잘림 처리

주요 텍스트:

```text
Game_Title
AI_Credits
Player_Credits
Round_Timer
Phase
Status_Message
Player_Expression
AI_Expression
Bet_Amount
```

한글이 네모로 나오거나 일부 글자가 보이지 않으면 해당 Text의 Font를 한글을 지원하는 폰트로 교체한다.

---

## 15. 이미지 적용 후 게임 테스트 순서

Unity 상단의 Play 버튼을 누르고 다음 순서로 확인한다.

1. AI 숫자 카드 2장이 앞면으로 보이는지 확인
2. AI 연산자·특수 카드 3장이 뒷면으로 보이는지 확인
3. 플레이어 왼쪽 아래 손패에 카드 5장과 `unopencard` 1장이 보이는지 확인
4. 손패 카드를 클릭하면 파란색 패널로 이동하는지 확인
5. 놓인 카드를 클릭하면 해당 카드와 뒤쪽 카드가 손패로 돌아오는지 확인
6. `unopencard`를 첫 번째·두 번째·세 번째 숫자 위치에 각각 놓을 수 있는지 확인
7. 잘못 선택했을 때 `초기화` 버튼 확인
8. 유효한 수식이 완성되는 즉시 `수식 확정` 버튼이 활성화되는지 확인
9. 카드 분배 직후 AI가 1억~5억을 먼저 베팅하는지 확인
10. 화면에 AI 베팅액과 `받기(CALL)`, `다이(FOLD)`만 표시되는지 확인
11. CALL 시 같은 금액이 적용되고 수식 작성으로 이동하는지 확인
12. FOLD 시 참가금 1억이 AI에게 이동하고 즉시 라운드가 끝나는지 확인
13. 카드 분배 마지막에 중앙 공용카드가 놓이고 유리잔이 위에서 아래로 내려오는지 확인
14. CALL 후 수식 제출 시 유리잔이 올라가고 공용 카드가 뒤집혀 실제 숫자가 공개되는지 확인
15. 결과 패널에서 AI·플레이어 수식과 총 판돈이 표시되는지 확인
16. 동점일 때 “묻고 더블로 가” 화면이 나오는지 확인
17. 한방 승부 승패가 최종 승리·패배로 연결되는지 확인

---

## 16. 자주 생기는 문제

### 이미지가 하얀 사각형으로 보임

- 이미지의 `Texture Type`이 `Sprite (2D and UI)`인지 확인
- `Source Image`에 Sprite가 들어갔는지 확인
- Image의 `Color` 알파가 0이 아닌지 확인

### 이미지 색상이 어둡거나 다른 색으로 보임

Image 컴포넌트의 `Color`가 기존 임시 색상으로 남아 있을 수 있다. 원본 색상을 사용하려면 흰색으로 바꾼다.

### 장식 이미지를 넣은 뒤 카드 클릭이 안 됨

장식 Image의 `Raycast Target`을 끈다. 특히 다음 오브젝트를 확인한다.

```text
ART_SLOT__Sealed_Shared_Frame
```

### 카드 이미지 위에 숫자나 기호가 겹침

- `EditableCard > Card_Value`와 `Card_Type`이 비활성화되어 있는지 확인한다.
- `FinalCardView` 컴포넌트가 최신 스크립트로 연결되어 있는지 확인한다.

### AI 연산 카드가 앞면으로 보임

`FinalCardView` 또는 `FinalGameView` 연결을 삭제했는지 확인한다. AI 행동 카드 컨테이너는 반드시 `AI_ActionCards__FACE_DOWN` 연결을 유지해야 한다.

### 버튼을 눌러도 작동하지 않음

- 버튼의 `Button` 컴포넌트가 삭제되지 않았는지 확인
- 장식 Image가 버튼 위에서 클릭을 막는지 확인
- EventSystem 오브젝트가 씬에 있는지 확인
- `FinalGameView`의 Inspector 참조가 `None`으로 바뀌지 않았는지 확인

---

## 17. 최종 저장

1. Prefab Mode에서 카드 이미지를 수정했다면 저장 후 씬으로 돌아온다.
2. `Ctrl+S`로 `MathHighLowGame` 씬을 저장한다.
3. Play Mode를 종료한 상태에서 저장되었는지 확인한다.
4. `Assets/Scenes/MathHighLowGame.unity`와 `Assets/Prefabs/EditableCard.prefab`을 별도로 백업한다.

Play Mode 중에 Inspector 값을 바꾸면 Play Mode 종료 시 원래 값으로 돌아갈 수 있다. 최종 수치와 배치는 반드시 Play Mode를 종료한 뒤 수정하고 저장한다.

---

## 빠른 에셋 삽입 위치 요약

| 넣으려는 이미지 | 선택할 오브젝트 |
|---|---|
| 전체 게임 배경 | `ART_SLOT__Background` |
| 숫자 `1~12` 개별 앞면 | `EditableCard > FinalCardView > Number 1~12 Sprite` |
| `+`, `-`, `÷`, `×`, `√` 개별 앞면 | `EditableCard > FinalCardView > Operators / Specials` |
| 카드 뒷면 | `EditableCard > ART_SLOT__Card_Back` |
| 공용 `?` 카드(unopencard) | `EditableCard > FinalCardView > Unopened Card Sprite` |
| AI 오른쪽 위 패 묶음 | 제거됨 |
| 플레이어 왼쪽 아래 손패 | 별도 이미지 불필요—실제 카드 6장 자동 배치 |
| 중앙 화투 더미 | 별도 이미지 불필요—`ART_SLOT__Card_Back` 7장 자동 사용 |
| 중앙 공용패 유리잔 | `SEALED_SHARED_CARD_AREA > ART_SLOT__Shared_Glass_Cover > Image > Source Image` |
| “묻고 더블로 가” 연출 | `FINAL_SHOWDOWN_PANEL__Editable > ART_SLOT__Final_Showdown` |
| 최종 승리 이미지 | `GAME_UI... > FinalGameView > Game Over Win Sprite` |
| 최종 패배 이미지 | `GAME_UI... > FinalGameView > Game Over Lose Sprite` |

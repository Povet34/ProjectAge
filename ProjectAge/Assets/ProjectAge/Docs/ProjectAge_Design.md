# ProjectAge — 설계 & 개발 로그

> 살아있는 문서. 작업 진행하면서 계속 갱신함.

## 1. 개요 / 목표
- 5 vs 5 물리 기반 파티 게임. 캐릭터를 **랙돌로 잡아** 휘두르고, 나이(가위바위보/미니게임)로 승부.
- 코어 재미 = **결합 물리**: A가 B를 잡으면 B는 랙돌, 여러 명이 한 명을 잡으면 `A → B ← C` 사슬 형태.
- 최종 배포: **Steam**. 네트워크는 **FishNet + 호스트 모드 + 스팀 릴레이**(서버비 0 지향). 지금은 미설치.

## 2. 이번 프로토타입 스코프 (로직 검증 단계, 네트워크 X)
1. **랙돌** — 캐릭터를 랙돌 상태로 토글 (조작상태 ↔ 랙돌).
2. **잡기 → 랙돌** — A가 B를 잡으면 B가 랙돌이 되고 A의 손에 매달림.
3. **다중 잡기** — 한 명(B)을 여러 명(A, C)이 동시에 잡는 `A → B ← C` 상황이 폭발/제로벡터 락 없이 안정적인지.
4. 그 외 파생 작업 = §7 백로그.

## 3. 아키텍처 (스크립트별 역할)
| 스크립트 | 위치 | 역할 |
|---|---|---|
| `GrabInput` (struct) + `Hand` (enum) | Runtime | 입력 스냅샷. **물리 로직 안에서 Input.* 폴링 금지**의 경계. 나중에 FishNet Replicate 데이터가 됨. |
| `RagdollController` | Runtime | 랙돌 on/off, 굳히기, 본 리지드바디 관리. `SetRagdoll(bool)`, `SetFrozen(bool)`, `HipsBody`, `GetNearestBody()`. |
| `Grabber` | Runtime | 잡기/놓기. 손 앵커(kinematic RB) ↔ 대상 본에 `SpringJoint` 생성. 권위자/테스트가 부르는 직접 API `Grab/Release`, 레이캐스트용 `TryGrabByAim`. |
| `GrabHandAnchor` (marker) | Runtime | 손 앵커 리지드바디 표식. RagdollController가 랙돌 본에서 제외. |
| `RagdollFactory` | Runtime | 프리미티브로 심플 휴머노이드 랙돌 생성. 테스트/샌드박스 더미용(에셋 의존 없음). |

## 4. 네트워크 이식 원칙 (지금부터 지킴)
1. **물리는 FixedUpdate/틱 기반** — 프레임 종속 금지. FishNet 예측이 틱 단위 재시뮬이라 그대로 이식되게.
2. **입력은 `GrabInput` 구조체로 주입** — Input 폴링을 물리 로직에 박지 않음. 이 구조체가 곧 네트워크 페이로드.
3. **권위자가 물리 계산, 나머지는 수신** — 로컬에선 내가 곧 권위자. 랙돌/결합물리는 한 곳(호스트)에서만 계산한다는 전제로 작성.
4. 잡힌 대상의 **오너십은 이관하지 않음** — 호스트가 조인트를 붙이는 방식이라 `A→B←C`가 조인트 2개로 자연 표현됨.

## 5. 잡기 물리 모델
- 손 앵커 = **kinematic Rigidbody**(손 뼈를 따라감). 잡으면 대상 본에 `SpringJoint`(spring/damper)로 손 앵커에 매닮.
- `A→B←C` = B의 같은 본에 SpringJoint 2개. 양쪽 스프링이 균형 → B가 중간에 매달림(제로벡터 하드락 아님, 폭발 아님).
- `breakForce` 유한값으로 두면 극한 장력에서 **자동 릴리즈**(줄다리기 해소의 한 방법). 기본은 Infinity로 두고 튜닝 대상.

## 6. 테스트 계획 (PlayMode / Test Runner)
- `Ragdoll_Toggle_SwitchesKinematicState` — 랙돌 토글 시 본 kinematic 상태 전환 검증. (스코프 1)
- `Grab_MakesTargetRagdoll_AndCreatesJoint` — 잡으면 대상 랙돌화 + 조인트 생성, 놓으면 제거. (스코프 2)
- `MultiGrab_TwoGrabbers_OneTarget_StaysStable` — A,C가 B를 동시에 잡을 때 조인트 2개 + NaN/폭발 없음 + 랙돌 유지. (스코프 3)

## 7. 진행 체크리스트
- [x] Sandbox 씬 생성
- [x] 코어 스크립트 (RagdollController / Grabber / RagdollFactory / GrabInput)
- [x] PlayMode 테스트 3종 작성
- [x] 컴파일 통과 + 테스트 그린 (PlayMode 3/3 pass, 2.69s)
- [x] 실제 Synty 캐릭터에 랙돌 빌드(RagdollBuilder, HumanBodyBones 11-body) — 3명 빌드 성공, 플레이모드에서 자연 낙하 + 잡기 빨림 확인
- [x] 3인칭 컨트롤러(이동 + 카메라) + 로컬 입력으로 잡기 (Sandbox 씬 조립 완료, 유저 인터랙티브 체감 대기)
- [x] 랙돌 스파이크/늘어남 버그 수정 — 자기충돌(비인접 본 캡슐 겹침)로 폭발하던 것을 `RagdollController.IgnoreSelfCollisions()`(런타임 Awake)로 해결. 이제 정상적으로 널브러짐(본 퍼짐 0.75~0.82m)
- [ ] 랙돌 비주얼 폴리시(선택) — 11-body라 살짝 뻣뻣, 플레이어는 애니메이터 컨트롤러 없어 T포즈. 본 추가/조인트 리밋 튜닝/컨트롤러 부착
- [ ] 로컬 다중 잡기 인터랙티브 하네스(봇에게 잡기 명령)
- [ ] 굳히기(E) → 사슬 메이스, 스윙
- [ ] FishNet 이식

## 8. 백로그 / 다음
- 굳히기(Freeze) 상태를 네트워크 SyncVar로 승격
- 랙돌 신체 1.5배 늘어남(조인트 linear limit)
- 나이/가위바위보/기둥 접촉 판정(서버 권위)
- 본 포즈 압축 동기화(전 본 X, 주요 본만)

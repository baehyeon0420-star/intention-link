# Intention-Link

ESP32 기반 MyoWare 2.0 근전도(EMG) 센서로 사용자의 손동작 의도를 인식하고, 이를 Unity 기반 XR 환경 및 로봇팔 제어에 실시간으로 매핑하는 시스템

## 개요

sEMG(표면 근전도) 신호를 실시간으로 수집·분류하여, 사용자의 "쥐기(Grip)" 동작 의도를 인식하고 이를 XR 상호작용(Confirm / Hold / Release) 및 로봇팔 명령으로 변환하는 프로젝트입니다. 머신러닝 기반 제스처 분류기와 Unity 클라이언트, 로봇팔 제어 모듈로 구성되어 있습니다.

## 시스템 구성

```
ESP32 + MyoWare 2.0(EMG 센서) → 시리얼 통신 → Unity(EMGSerialReader)
                                                  │
                                    ┌─────────────┴─────────────┐
                              EMGMLClassifier              EMGStateManager
                            (그립/휴지 상태 분류)         (Confirm/Hold/Release)
                                    │                             │
                          RobotCommandMapper              XR 상호작용 / 오브젝트 제어
                                    │
                            RobotArmController
```

## 구성 요소

| 구분 | 내용 |
|---|---|
| 하드웨어 | ESP32 DevKitC V4, MyoWare 2.0 Muscle Sensor |
| 신호 수집 | Python (`emg_data_collection/`) — 시리얼 데이터 수집, 특징 추출, 모델 학습 |
| 분류 모델 | RandomForest / Logistic Regression (Leave-One-Group-Out 교차검증) |
| 엔진 | Unity (OpenXR 지원 예정) |
| 대상 플랫폼 | XR 상호작용, 로봇팔(ARMISTEEL) 제어 |

## 폴더 구조

```
intention-link/
├── Assets/
│   ├── Scripts/
│   │   ├── EMG/          # EMG 시리얼 수신, ML 분류, 상태 관리
│   │   └── robot/         # 로봇팔 명령 매핑 및 제어
│   └── Scenes/            # Main, ARMISTEEL_Demo 등 데모 씬
├── emg_data_collection/   # 데이터 수집·특징추출·모델 학습 (Python)
│   ├── collect.py         # ESP32 시리얼로부터 EMG 원시 데이터 수집
│   ├── features.py        # 200ms 윈도우 기반 특징 추출
│   ├── train.py           # 분류기 학습 (RandomForest/LogisticRegression)
│   ├── analyze.py         # 수집 데이터 분석
│   └── s1~s4_*.csv        # 피험자별 grip/rest 라벨링 데이터
└── Packages, ProjectSettings 등 Unity 표준 구성
```

## 주요 스크립트

| 파일 | 역할 |
|---|---|
| `EMGSerialReader.cs` | ESP32로부터 시리얼로 EMG 값 수신 |
| `EMGMLClassifier.cs` | 학습된 모델 기반 실시간 제스처(그립/휴지) 분류 |
| `EMGStateManager.cs` | Confirm / Hold / Release 상태 전이 관리 |
| `KeyboardEMGSimulator.cs` | 하드웨어 없이 키보드로 EMG 입력 시뮬레이션(테스트용) |
| `RobotCommandMapper.cs` | 분류된 제스처를 로봇팔 명령으로 변환 |
| `RobotArmController.cs` | 로봇팔 실제 구동 제어 |

## 진행 상황

- [x] ESP32-Unity 시리얼 통신
- [x] EMG 데이터 수집 파이프라인 (4명 피험자, grip/rest)
- [x] 특징 추출 및 ML 분류기 학습
- [x] 실시간 제스처 상태(Confirm/Hold/Release) 로직
- [x] 로봇팔 명령 매핑 연동
- [ ] OpenXR 기반 손 추적/시선 기반 상호작용
- [ ] 분류 정확도 개선 및 다중 제스처 확장

## 관련 자료
- [sEMG 진폭 편차 정규화를 통한 실시간 수축 강도 시각화 논문 (PDF)](./paper/sEMG_ContractionIntensity_Normalization.pdf) — 본 프로젝트의 EMG 신호처리 이론적 배경

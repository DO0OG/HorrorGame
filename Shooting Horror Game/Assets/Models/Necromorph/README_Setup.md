# Necromorph 몬스터 에셋 — Unity 적용 가이드

## 파일 목록

| 파일 | 해상도 | 용도 |
|---|---|---|
| `SK_Necromorph.fbx` | — | 메시 본체 (95,786 폴리 / 87,194 버텍스, 높이 4.20m) |
| `T_Necromorph_BaseColor.png` | 4096² | 알베도 (sRGB) |
| `T_Necromorph_Normal.png` | 4096² | 노멀맵 (OpenGL 규격 = +Y, Non-Color) |
| `T_Necromorph_MaskMap.png` | 4096² | URP Lit 마스크맵 — R=Metallic, G=Occlusion, B=Detail, A=Smoothness |
| `T_Necromorph_Roughness.png` | 4096² | 러프니스 단독 (Built-in / 커스텀 셰이더용) |
| `T_Necromorph_AO.png` | 4096² | AO 단독 |

바운딩 박스: X 2.08m × Y 1.80m × Z 4.20m. 원점은 발바닥, 정면 -Z(Unity forward), 업 +Y.

## 임포트 설정

**FBX**
- Scale Factor `1`, Convert Units 체크
- Normals: `Import`, Tangents: `Calculate Mikktspace`
- 스무딩 그룹이 포함되어 있어 별도 조정 불필요

**텍스처**
- `BaseColor` → sRGB **체크**
- `Normal` → Texture Type: `Normal map`
- `MaskMap` / `Roughness` / `AO` → sRGB **해제**

> 4096²는 캐릭터 하나당 VRAM을 꽤 먹습니다. 씬에 다수 배치할 계획이면
> 임포터에서 Max Size를 2048로 내려도 육안 차이는 거의 없습니다.

## 머티리얼 (URP Lit)

머티리얼 슬롯은 **1개**입니다 (`M_Necromorph_Baked`).

- Base Map → `T_Necromorph_BaseColor`
- Normal Map → `T_Necromorph_Normal`
- Mask Map → `T_Necromorph_MaskMap`
- Metallic `0`, Smoothness Source: `Metallic Alpha`

## 씬 배치 시 주의

- 기존 `Monster` 오브젝트는 Scale `2.0`에 높이 약 3m입니다.
  이 모델은 **Scale 1.0에서 이미 4.20m**이므로 추가 스케일링이 필요 없습니다.
- `CapsuleCollider`: Height `4.20`, Radius `0.45`, Center `(0, 2.10, 0)`
  - X폭 2.08m는 어깨에서 뻗어나온 가시까지 포함한 값입니다. 콜라이더는
    몸통 기준(반경 약 0.45)으로 잡아야 좁은 복도에서 끼지 않습니다.
- `NavMeshAgent`: Height `4.20`, Radius `0.5`
  - NavMesh Surface의 Agent Height도 함께 올려야 경로가 생성됩니다.

## 메시에 포함된 부가 데이터

`ClothMask` 라는 버텍스 컬러 어트리뷰트가 들어 있습니다.
하반신 넝마 영역이 1.0, 나머지가 0.0입니다. 피격 시 천만 다르게 반응시키거나
데칼·디졸브 범위를 분리하고 싶을 때 셰이더에서 바로 쓸 수 있습니다.

## 남은 작업 — 리깅

이 에셋은 **리깅되지 않은 정적 메시**입니다. 기존 `Monster.controller`
애니메이션을 쓰려면 다음이 필요합니다.

1. 블렌더에서 Rigify 또는 수동 아마추어로 휴머노이드 본 배치
2. Automatic Weights로 스키닝 (어깨 가시는 별도 본 없이 상체 본에 웨이트)
3. FBX 재익스포트 후 Unity에서 Rig → `Humanoid` 설정
4. `Monster` 프리팹의 `SkinnedMeshRenderer` 교체

## 라이선스 — 크레딧 표기 필수

베이스 메시 출처:

> **"Horror Humanoid Creature"** by **Purple.Point**
> https://sketchfab.com/3d-models/b5874b20f8c34b919a52eb3cb7dad94c
> Licensed under **CC Attribution (CC BY 4.0)**

CC BY는 **원저작자 표기가 의무**입니다. 게임 크레딧 화면이나 배포 패키지의
라이선스 문서에 위 3줄을 반드시 포함시켜야 합니다. 표기 없이 배포하면
라이선스 위반입니다.

텍스처와 머티리얼(살점 톤, 핏자국, 오염, 습기 표현)은 이 프로젝트에서
새로 제작해 베이크한 것이라 별도 제약이 없습니다. **메시 형상 자체가**
CC BY 대상입니다.

> 참고: 검색 범위 내 Sketchfab의 다운로드 가능한 호러 크리처 중
> CC0(출처 표기 불필요)는 존재하지 않았습니다. 표기 의무를 없애려면
> Unity Asset Store 등 유료 마켓의 로열티 프리 에셋을 구매해야 합니다.

Streaming Optimized 
```radarr:
# Set the URL/API Key to your actual instance
  - base_url: http://xxx.xxx.xxx.xxxX:7878 #Place your Ronarr Url here
    api_key: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
    quality_definition:
      type: movie
    # Custom Format Settings
    delete_old_custom_formats: false
    custom_formats:
      - trash_ids:
#HQ Source Groups
          - 26fa26253af4001701fedb56cec376dc # HQ-WEBDL
#Misc
          - e7718d7a3ce595f289bfee26adc178f5 # Repack/Proper
          - 0d91270a7255a1e388fa85e959f359d8 # FreeLeech (use if you want to prefer FreeLeech in a tie breaker situation)
          - 4a3b087eea2ce012fcc1ce319259a3be #Anime Dual Audio (If using anime in a hybrid instance tags should be used)
#Streaming Services
          - b3b3a6ac74ecbd56bcdbefa4799fb9df #Amazon
          - 3472d276482257d68f7836a55ca24877 #Apple TV+
          - 84272245b2988854bfb76a16e60baea5 #Disney+
          - 5763d1b0ce84aff3b21038eea8e9b8ad #HBO Max
          - 526d445d4c16214309f0fd2b3be18a89 #Hulu
          - 170b1d363bd8516fbf3a3eb05d4faff6 #Netflix
          - e36a0ba1bc902b26ee40818a1d59b8bd #Paramount+
          - c9fd353f8f5f1baf56dc601c4cb29920 #Peacock TV
#unwanted
          - b8cd450cbfa689c0259a01d9e29ba3d6 # 3D
          - ed38b889b31be83fda192888e2286d83 # BR-DISK
          - 90a6f9a284dff5103f6346090e6280c8 # LQ
          - ae9b7c9ebde1f3bd336a8cbd1ec4c5e5 # No-RlsGroup
          - 7357cf5161efbf8c4d5d0c30b4815ee2 # Obfuscated
          - 90cedc1fea7ea5d11298bebd3d1d3223 # EVO (no WEBDL)
          - 923b6abef9b17f937fab56cfcf89e1f1 # DV (WEBDL)
          - b2be17d608fc88818940cd1833b0b24c # x265 (720/1080p)

        quality_profiles:
          - name: Any
            reset_unmatched_scores: true
      - trash_ids:
          - 1c7d7b04b15cc53ea61204bebbcc1ee2 # HQ 0
          - 403f3f6266b90439cacc1e07cae4dc2d # HQ-Remux 0
```
Hybrid Sonarr Config
```sonarr:
    # Set the URL/API Key to your actual instance
  - base_url: http://xxx.xxx.xxx.xxx:8989 #Place your Sonarr Url here
    api_key: xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

    # Quality definitions from the guide to sync to Sonarr. Choice: anime, series, hybrid
    quality_definition: hybrid

    # Release profiles from the guide to sync to Sonarr.
    # You can optionally add tags and make negative scores strictly ignored
    release_profiles:
      # Series
      - trash_ids:
          - EBC725268D687D588A20CBC5F97E538B # Low Quality Groups
          - 1B018E0C53EC825085DD911102E2CA36 # Release Sources (Streaming Service)
          - 71899E6C303A07AF0E4746EFF9873532 # P2P Groups + Repack/Proper
          - d428eda85af1df8904b4bbe4fc2f537c # Anime - First release profile
          - 6cd9e10bb5bb4c63d2d7cd3279924c7b # Anime - Second release profile
          - EBC725268D687D588A20CBC5F97E538B # Low Quality Groups
          - 1B018E0C53EC825085DD911102E2CA36 # Release Sources (Streaming Service)
          - 71899E6C303A07AF0E4746EFF9873532 # P2P Groups + Repack/Proper
      - trash_ids: [76e060895c5b8a765c310933da0a5357] # Optionals
        filter:
          include:
            - cec8880b847dd5d31d29167ee0112b57 # Golden rule
            - f3f0f3691c6a1988d4a02963e69d11f2 # Ignore The Group -SCENE
            - 436f5a7d08fbf02ba25cb5e5dfe98e55 # Ignore Dolby Vision without HDR10 fallback.
            - 6f2aefa61342a63387f2a90489e90790 # Dislike retags: rartv, rarbg, eztv, TGx
            - 19cd5ecc0a24bf493a75e80a51974cdd # Dislike retagged groups
            - 6a7b462c6caee4a991a9d8aa38ce2405 # Dislike release ending: en
            - 236a3626a07cacf5692c73cc947bc280 # Dislike release containing: 1-```
            
            

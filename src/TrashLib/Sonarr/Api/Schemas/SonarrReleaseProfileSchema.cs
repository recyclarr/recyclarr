namespace TrashLib.Sonarr.Api.Schemas;

public static class SonarrReleaseProfileSchema
{
    public static string V1 => @"{
  'definitions': {
    'SonarrPreferredTerm': {
      'type': [
        'object',
        'null'
      ],
      'properties': {
        'key': {
          'type': [
            'string',
            'null'
          ]
        },
        'value': {
          'type': 'integer'
        }
      }
    }
  },
  'type': 'object',
  'properties': {
    'id': {
      'type': 'integer'
    },
    'enabled': {
      'type': 'boolean'
    },
    'name': {
      'type': [
        'string',
        'null'
      ]
    },
    'required': {
      'type': [
        'string',
        'null'
      ]
    },
    'ignored': {
      'type': [
        'string',
        'null'
      ]
    },
    'preferred': {
      'type': [
        'array',
        'null'
      ],
      'items': {
        '$ref': '#/definitions/SonarrPreferredTerm'
      }
    },
    'includePreferredWhenRenaming': {
      'type': 'boolean'
    },
    'indexerId': {
      'type': 'integer'
    },
    'tags': {
      'type': [
        'array',
        'null'
      ],
      'items': {
        'type': 'integer'
      }
    }
  }
}";

    public static string V2 => @"{
  'definitions': {
    'SonarrPreferredTerm': {
      'type': [
        'object',
        'null'
      ],
      'properties': {
        'key': {
          'type': [
            'string',
            'null'
          ]
        },
        'value': {
          'type': 'integer'
        }
      }
    }
  },
  'type': 'object',
  'properties': {
    'id': {
      'type': 'integer'
    },
    'enabled': {
      'type': 'boolean'
    },
    'name': {
      'type': [
        'string',
        'null'
      ]
    },
    'required': {
      'type': [
        'array',
        'null'
      ],
      'items': {
        'type': [
          'string',
          'null'
        ]
      }
    },
    'ignored': {
      'type': [
        'array',
        'null'
      ],
      'items': {
        'type': [
          'string',
          'null'
        ]
      }
    },
    'preferred': {
      'type': [
        'array',
        'null'
      ],
      'items': {
        '$ref': '#/definitions/SonarrPreferredTerm'
      }
    },
    'includePreferredWhenRenaming': {
      'type': 'boolean'
    },
    'indexerId': {
      'type': 'integer'
    },
    'tags': {
      'type': [
        'array',
        'null'
      ],
      'items': {
        'type': 'integer'
      }
    }
  }
}
";
}

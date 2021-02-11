from .orderedenum import OrderedEnum

class Severity(OrderedEnum):
    Info = 1
    Debug = 2

class Logger:
    def __init__(self, args):
        self.severity = Severity.Debug if args.debug else Severity.Info

    def info(self, msg):
        print(msg)

    def debug(self, msg):
        if self.severity >= Severity.Debug:
            print(msg)
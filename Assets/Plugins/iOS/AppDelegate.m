#import "UnityAppController.h"

@interface MyUnityBridge : NSObject
// 定义从 Unity 传递过来的消息处理方法
- (void)callObjectiveCMethod:(NSString *)message;
@end

@implementation MyUnityBridge

- (void)callObjectiveCMethod:(NSString *)message {
    // 在这里处理 Unity 传过来的消息
    NSLog(@"Received message from Unity: %@", message);
    
    // 你可以在这里执行其他操作，比如调用其他本地功能，或者使用 UIAlertController 显示对话框
    UIAlertController *alert = [UIAlertController alertControllerWithTitle:@"Unity Message" 
                                                                   message:message 
                                                            preferredStyle:UIAlertControllerStyleAlert];
    
    UIAlertAction *okAction = [UIAlertAction actionWithTitle:@"OK" 
                                                       style:UIAlertActionStyleDefault 
                                                     handler:nil];
    [alert addAction:okAction];
    
    // 获取当前的 ViewController 并显示 alert
    UIViewController *rootViewController = [UnityGetGLViewController() rootViewController];
    [rootViewController presentViewController:alert animated:YES completion:nil];
}

@end

// 在 Unity 中调用此方法时，iOS 会调用这个方法
extern "C" void CallObjectiveCMethod(const char* message) {
    NSString *nsMessage = [NSString stringWithUTF8String:message];
    MyUnityBridge *bridge = [[MyUnityBridge alloc] init];
    [bridge callObjectiveCMethod:nsMessage];
}

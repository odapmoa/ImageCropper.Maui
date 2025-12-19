using Bind_TOCropViewController;
using CoreGraphics;
using Foundation;
using System.Diagnostics;
using UIKit;


namespace ImageCropper.Maui
{
    // All the code in this file is only included on iOS.
    public class PlatformImageCropper : IImageCropperWrapper
    {
        public void ShowFromFile(ImageCropper imageCropper, string imageFile)
        {

            UIImage image = UIImage.FromFile(imageFile);

            TOCropViewController cropViewController;

            if (imageCropper.CropShape == ImageCropper.CropShapeType.Oval)
            {
                cropViewController = new TOCropViewController(TOCropViewCroppingStyle.Circular, image);
            }
            else
            {
                cropViewController = new TOCropViewController(image);
            }
            cropViewController.DoneButtonTitle = imageCropper.CropButtonTitle;
            cropViewController.CancelButtonTitle = imageCropper.CancelButtonTitle;

            if (imageCropper.AspectRatioX > 0 && imageCropper.AspectRatioY > 0)
            {
                cropViewController.AspectRatioPreset = TOCropViewControllerAspectRatioPreset.Custom;
                cropViewController.ResetAspectRatioEnabled = false;
                cropViewController.AspectRatioLockEnabled = true;
                cropViewController.CustomAspectRatio = new CGSize(imageCropper.AspectRatioX, imageCropper.AspectRatioY);
            }

            cropViewController.OnDidCropToRect = (outImage, cropRect, angle) =>
            {
                Finalize(imageCropper, outImage);
            };

            cropViewController.OnDidCropToCircleImage = (outImage, cropRect, angle) =>
            {
                Finalize(imageCropper, outImage);
            };

            cropViewController.OnDidFinishCancelled = (cancelled) =>
            {
                imageCropper.Failure?.Invoke();
                UIApplication.SharedApplication.KeyWindow.RootViewController.DismissViewController(true, null);
            };

            // Fix for iOS Safe Area issue:
            // 1. Set ModalPresentationStyle to FullScreen to avoid page sheet modal style
            cropViewController.ModalPresentationStyle = UIModalPresentationStyle.FullScreen;

            // 2. Add additional safe area insets to move toolbar above home indicator
            // Get the bottom safe area inset from the key window
            var window = UIApplication.SharedApplication.KeyWindow;
            var bottomInset = window?.SafeAreaInsets.Bottom ?? 0;
            if (bottomInset > 0)
            {
                // Add extra padding to ensure toolbar buttons are above the home indicator
                cropViewController.AdditionalSafeAreaInsets = new UIEdgeInsets(0, 0, bottomInset, 0);
            }

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(cropViewController, true, null);

            if(imageCropper.PageTitle != null && imageCropper.PageTitle.Length > 0)
            {
                cropViewController.Title = imageCropper.PageTitle;
            }
        }
        private static async void Finalize(ImageCropper imageCropper, UIImage image)
        {
            string documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string jpgFilename = System.IO.Path.Combine(documentsDirectory, Guid.NewGuid().ToString() + ".jpg");
            NSData imgData = image.AsJPEG();
            NSError err;

            // small delay
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromMilliseconds(100));
            if (imgData.Save(jpgFilename, false, out err))
            {
                imageCropper.Success?.Invoke(jpgFilename);
            }
            else
            {
                Debug.WriteLine("NOT saved as " + jpgFilename + " because" + err.LocalizedDescription);
                imageCropper.Failure?.Invoke();
            }
            UIApplication.SharedApplication.KeyWindow.RootViewController.DismissViewController(true, null);
        }
    }
}
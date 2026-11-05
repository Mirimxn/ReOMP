import { ToastOptions, toast } from "react-toastify";

export const customToast = {
  info: (content: string, options: ToastOptions<string>) => {
    toast.info(content, {
      position: `top-center`,
      closeButton: false,
      theme: `colored`,
      hideProgressBar: true,
      style: {
        fontFamily: `lego`,
        fontSize: `1.2rem`,
        textAlign: `center`,
      },
      icon: false,
      autoClose: 2000,
      ...options,
    });
  },
};

import sys
import json
from openai import OpenAI, APIError, APIConnectionError, AuthenticationError
from config import Config


def check_connection() -> dict:
    result = {
        "status": "error",
        "model": Config.EXTRACTOR_MODEL,
        "message": "",
    }

    if not Config.OPENROUTER_API_KEY:
        result["message"] = "Нет API ключа"
        return result
    if Config.OPENROUTER_API_KEY == "sk-or-v1-твой_ключ_здесь":
        result["message"] = "Ключ не изменён (значение по умолчанию)"
        return result

    try:
        client = OpenAI(
            base_url=Config.OPENROUTER_BASE,
            api_key=Config.OPENROUTER_API_KEY,
            default_headers={
                "HTTP-Referer": "https://github.com/brain",
                "X-Title": "Brain Digital Employee",
            },
        )
        response = client.chat.completions.create(
            model=Config.EXTRACTOR_MODEL,
            messages=[{"role": "user", "content": "Ответь одним словом: ok"}],
            max_tokens=10,
            temperature=0,
        )
        result["status"] = "ok"
        result["message"] = f"Подключено: {Config.EXTRACTOR_MODEL}"
        result["reply"] = response.choices[0].message.content
    except AuthenticationError:
        result["message"] = "Неверный API ключ"
    except APIConnectionError:
        result["message"] = "Нет доступа к openrouter.ai (проверь интернет)"
    except APIError as e:
        if e.status_code == 402:
            result["message"] = "Недостаточно средств на балансе OpenRouter"
        else:
            result["message"] = f"Ошибка API: {e.message[:100]}"
    except Exception as e:
        result["message"] = f"Ошибка: {str(e)[:100]}"

    return result


if __name__ == "__main__":
    res = check_connection()
    print(json.dumps(res, ensure_ascii=False, indent=2))
    sys.exit(0 if res["status"] == "ok" else 1)

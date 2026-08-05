---
name: conventional-commit
description: Genera mensajes de commit siguiendo el estandar Conventional Commits de git cuando el usuario lo solicita.
---

# Conventional Commit
1. Verifica que cambios hubieron en el proyecto usando git diff
2. Identifica el tipo de cambio que se realizó (feat, fix, docs, style, refactor, test, chore)
3. Escribe un mensaje de commit siguiendo el formato: `<tipo><mensaje>`, por ejemplo: `feat: agregar nueva funcionalidad`. El mensaje debe estar en minuscula, en español, no debe exceder los 100 caracteres y sin punto al final

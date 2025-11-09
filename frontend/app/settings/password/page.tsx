"use client"

import type React from "react"

import { useState } from "react"
import { motion } from "framer-motion"
import { Eye, EyeOff, Shield, Lock, Check, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import Header from "@/components/header"

export default function PasswordPage() {
  const [formData, setFormData] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  })

  const [showPasswords, setShowPasswords] = useState({
    current: false,
    new: false,
    confirm: false,
  })

  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null)

  const passwordRequirements = [
    { text: "Au moins 8 caractères", met: formData.newPassword.length >= 8 },
    { text: "Une majuscule", met: /[A-Z]/.test(formData.newPassword) },
    { text: "Une minuscule", met: /[a-z]/.test(formData.newPassword) },
    { text: "Un chiffre", met: /\d/.test(formData.newPassword) },
    { text: "Un caractère spécial", met: /[!@#$%^&*(),.?":{}|<>]/.test(formData.newPassword) },
  ]

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target
    setFormData({ ...formData, [name]: value })
    if (message) setMessage(null)
  }

  const togglePasswordVisibility = (field: keyof typeof showPasswords) => {
    setShowPasswords((prev) => ({ ...prev, [field]: !prev[field] }))
  }

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()

    if (formData.newPassword !== formData.confirmPassword) {
      setMessage({ type: "error", text: "Les mots de passe ne correspondent pas" })
      return
    }

    if (!passwordRequirements.every((req) => req.met)) {
      setMessage({ type: "error", text: "Le mot de passe ne respecte pas tous les critères" })
      return
    }

    // Simulate password change
    setMessage({ type: "success", text: "Mot de passe modifié avec succès" })
    setFormData({
      currentPassword: "",
      newPassword: "",
      confirmPassword: "",
    })
  }

  return (
    <div className="min-h-screen bg-black">
      <Header title="Modifier le mot de passe" back />

      <div className="px-4 py-6 space-y-6">
        {/* Header Info */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          className="bg-gray-900/50 backdrop-blur-xs rounded-2xl p-6 border border-gray-800"
        >
          <div className="flex items-center space-x-4">
            <div className="w-12 h-12 bg-gray-800 rounded-xl flex items-center justify-center">
              <Shield className="w-6 h-6 text-orange-400" />
            </div>
            <div>
              <h2 className="text-xl font-semibold text-white">Sécurité du compte</h2>
              <p className="text-gray-400">Modifiez votre mot de passe pour sécuriser votre compte</p>
            </div>
          </div>
        </motion.div>

        {/* Form */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="bg-gray-900/50 backdrop-blur-xs rounded-2xl p-6 border border-gray-800"
        >
          <form onSubmit={handleSubmit} className="space-y-6">
            {message && (
              <motion.div
                initial={{ opacity: 0, y: -10 }}
                animate={{ opacity: 1, y: 0 }}
                className={`p-4 rounded-xl ${
                  message.type === "success"
                    ? "bg-green-900/30 text-green-400 border border-green-800/30"
                    : "bg-red-900/30 text-red-400 border border-red-800/30"
                }`}
              >
                <div className="flex items-center space-x-2">
                  {message.type === "success" ? <Check className="w-5 h-5" /> : <X className="w-5 h-5" />}
                  <span>{message.text}</span>
                </div>
              </motion.div>
            )}

            <div>
              <Label htmlFor="currentPassword" className="text-white font-medium">
                Mot de passe actuel
              </Label>
              <div className="relative mt-2">
                <Input
                  id="currentPassword"
                  name="currentPassword"
                  type={showPasswords.current ? "text" : "password"}
                  value={formData.currentPassword}
                  onChange={handleChange}
                  className="bg-gray-800 border-gray-700 text-white pr-12"
                  required
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
                  onClick={() => togglePasswordVisibility("current")}
                >
                  {showPasswords.current ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>

            <div>
              <Label htmlFor="newPassword" className="text-white font-medium">
                Nouveau mot de passe
              </Label>
              <div className="relative mt-2">
                <Input
                  id="newPassword"
                  name="newPassword"
                  type={showPasswords.new ? "text" : "password"}
                  value={formData.newPassword}
                  onChange={handleChange}
                  className="bg-gray-800 border-gray-700 text-white pr-12"
                  required
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
                  onClick={() => togglePasswordVisibility("new")}
                >
                  {showPasswords.new ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>

              {/* Password Requirements */}
              {formData.newPassword && (
                <div className="mt-4 space-y-2">
                  <p className="text-sm text-gray-400 font-medium">Critères du mot de passe :</p>
                  {passwordRequirements.map((req, index) => (
                    <div key={index} className="flex items-center space-x-2">
                      {req.met ? <Check className="w-4 h-4 text-[#00E26D]" /> : <X className="w-4 h-4 text-red-400" />}
                      <span className={`text-sm ${req.met ? "text-[#00E26D]" : "text-gray-400"}`}>{req.text}</span>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div>
              <Label htmlFor="confirmPassword" className="text-white font-medium">
                Confirmer le nouveau mot de passe
              </Label>
              <div className="relative mt-2">
                <Input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showPasswords.confirm ? "text" : "password"}
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  className="bg-gray-800 border-gray-700 text-white pr-12"
                  required
                />
                <button
                  type="button"
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
                  onClick={() => togglePasswordVisibility("confirm")}
                >
                  {showPasswords.confirm ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
              {formData.confirmPassword && formData.newPassword !== formData.confirmPassword && (
                <p className="text-red-400 text-sm mt-2 flex items-center">
                  <X className="w-4 h-4 mr-1" />
                  Les mots de passe ne correspondent pas
                </p>
              )}
            </div>

            <Button
              type="submit"
              className="w-full bg-[#00E26D] hover:bg-[#00E26D]/80 text-black h-12 font-medium"
              disabled={
                !passwordRequirements.every((req) => req.met) || formData.newPassword !== formData.confirmPassword
              }
            >
              <Lock className="w-4 h-4 mr-2" />
              Mettre à jour le mot de passe
            </Button>
          </form>
        </motion.div>

        {/* Security Tips */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="bg-blue-900/20 backdrop-blur-xs rounded-2xl p-6 border border-blue-800/30"
        >
          <h3 className="text-lg font-semibold text-white mb-4">Conseils de sécurité</h3>
          <ul className="space-y-2 text-gray-400 text-sm">
            <li>• Utilisez un mot de passe unique pour chaque compte</li>
            <li>• Évitez d'utiliser des informations personnelles</li>
            <li>• Changez votre mot de passe régulièrement</li>
            <li>• Utilisez un gestionnaire de mots de passe</li>
          </ul>
        </motion.div>
      </div>
    </div>
  )
}
